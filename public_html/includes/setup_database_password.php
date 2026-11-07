<?php
declare(strict_types=1);
/** Local setup only. Password administration is never run by normal game requests. */
if(PHP_SAPI!=='cli'&&realpath((string)($_SERVER['SCRIPT_FILENAME']??''))===__FILE__){http_response_code(404);exit;}

final class DnSetupDatabasePasswordException extends RuntimeException {
    public readonly string $reason;
    public function __construct(string $reason){
        $this->reason=$reason;
        $messages=[
            'local_only'=>'Database password setup is available only through localhost on the server PC.',
            'local_root_required'=>'Automatic XAMPP password setup requires the local root database account. Check the database host and user in config/app.php.',
            'invalid_password_input'=>'The current database password must contain at most 4,096 bytes.',
            'configuration_unavailable'=>'The installed config/app.php could not be read or updated. Check its file permissions before trying again.',
            'configuration_format'=>'The database configuration must be a literal db array in config/app.php. Keep its pass setting as a quoted string or omit that setting.',
            'setup_busy'=>'Another local database setup is running. Wait for it to finish, then try again.',
            'cannot_authenticate'=>'The local database rejected the available root passwords. Enter its current root password below, then try again.',
            'database_unavailable'=>'The local database server could not be reached. Start MySQL in XAMPP and check the configured host and port.',
            'account_metadata'=>'The local root accounts could not be checked. Use a root account permitted to manage database accounts.',
            'unsupported_authentication'=>'This root account does not support a stored database password. Its existing authentication method was preserved.',
            'password_update_failed'=>'The database rejected the password update. Check its password policy and account-management permissions; no schema action was started.',
            'password_verification_failed'=>'The updated database password could not be verified over a fresh local TCP connection. No schema action was started. Retry setup to finish synchronizing the connection.',
            'configuration_commit_failed'=>'The database password was updated, but config/app.php could not be replaced. Check its file permissions and retry setup before opening the game.',
            'partial_sync'=>'The main database connection and game configuration are synchronized, but another local root account could not be updated. No schema action was started. Check account-management permissions and retry setup.',
        ];
        parent::__construct($messages[$reason]??'Local database password setup could not finish. No schema action was started.');
    }
}
function dn_setup_password_failure(string $reason): never {throw new DnSetupDatabasePasswordException($reason);}
function dn_setup_password_target(): string {return 'QuestionMark123456?';}

/** Tokens carry byte positions so comments, spacing and unrelated settings survive. */
function dn_setup_password_tokens(string $source): array {
    try{$raw=token_get_all($source,TOKEN_PARSE);}catch(Throwable){dn_setup_password_failure('configuration_format');}
    $tokens=[];$offset=0;
    foreach($raw as $token){$text=is_array($token)?$token[1]:$token;$id=is_array($token)?$token[0]:null;$tokens[]=['id'=>$id,'text'=>$text,'start'=>$offset,'end'=>$offset+strlen($text)];$offset+=strlen($text);}
    return $tokens;
}
function dn_setup_password_significant(array $tokens,int $start,int $end): array {
    $out=[];for($i=$start;$i<$end;$i++)if(!in_array($tokens[$i]['id'],[T_WHITESPACE,T_COMMENT,T_DOC_COMMENT],true))$out[]=$i;return $out;
}
function dn_setup_password_array_bounds(array $tokens,int $start): array {
    $i=$start;
    if(($tokens[$i]['id']??null)===T_ARRAY){$next=dn_setup_password_significant($tokens,$i+1,count($tokens));$i=$next[0]??-1;if(($tokens[$i]['text']??'')!=='(')dn_setup_password_failure('configuration_format');}
    elseif(($tokens[$i]['text']??'')!=='[')dn_setup_password_failure('configuration_format');
    $open=$i;$stack=[];
    for(;$i<count($tokens);$i++){
        if($tokens[$i]['id']!==null)continue;$text=$tokens[$i]['text'];
        if(in_array($text,['[','(','{'],true))$stack[]=$text;
        elseif(in_array($text,[']',')','}'],true)){
            $expected=[']'=>'[',')'=>'(','}'=>'{'][$text];if(array_pop($stack)!==$expected)dn_setup_password_failure('configuration_format');
            if(!$stack)return [$open,$i];
        }
    }
    dn_setup_password_failure('configuration_format');
}
function dn_setup_password_literal(string $text): string {
    $body=substr($text,1,-1);return $text[0]==="'"?str_replace(["\\'","\\\\"],["'","\\"],$body):stripcslashes($body);
}
function dn_setup_password_entries(array $tokens,int $open,int $close): array {
    $entries=[];$start=$open+1;$depth=0;
    for($i=$start;$i<=$close;$i++){
        $text=$tokens[$i]['text'];$boundary=$i===$close||($tokens[$i]['id']===null&&$text===','&&$depth===0);
        if($boundary){
            $part=dn_setup_password_significant($tokens,$start,$i);$start=$i+1;if(!$part)continue;
            if($tokens[$part[0]]['id']===T_ELLIPSIS)dn_setup_password_failure('configuration_format');
            if(count($part)<3||$tokens[$part[0]]['id']!==T_CONSTANT_ENCAPSED_STRING||$tokens[$part[1]]['id']!==T_DOUBLE_ARROW)dn_setup_password_failure('configuration_format');
            $keyToken=$tokens[$part[0]]['text'];if(str_contains($keyToken,'\\'))dn_setup_password_failure('configuration_format');
            $key=dn_setup_password_literal($keyToken);if(isset($entries[$key]))dn_setup_password_failure('configuration_format');$entries[$key][]=array_slice($part,2);
        }elseif($tokens[$i]['id']===null){if(in_array($text,['[','(','{'],true))$depth++;elseif(in_array($text,[']',')','}'],true))$depth--;}
    }
    return $entries;
}
/** Only the db.pass literal is replaced. A missing key is inserted in that array. */
function dn_setup_password_config_source(string $source,string $password): string {
    $tokens=dn_setup_password_tokens($source);$returns=[];$depth=0;
    foreach($tokens as $i=>$token){
        if($token['id']===T_RETURN&&$depth===0)$returns[]=$i;
        if($token['id']===null){if(in_array($token['text'],['[','(','{'],true))$depth++;elseif(in_array($token['text'],[']',')','}'],true))$depth--;}
    }
    if(count($returns)!==1)dn_setup_password_failure('configuration_format');
    $after=dn_setup_password_significant($tokens,$returns[0]+1,count($tokens));[$open,$close]=dn_setup_password_array_bounds($tokens,$after[0]??-1);
    $tail=dn_setup_password_significant($tokens,$close+1,count($tokens));if(!$tail||$tokens[$tail[0]]['text']!==';')dn_setup_password_failure('configuration_format');
    $entries=dn_setup_password_entries($tokens,$open,$close);
    if(count($entries['db']??[])!==1)dn_setup_password_failure('configuration_format');
    $db=$entries['db'][0];[$dbOpen,$dbClose]=dn_setup_password_array_bounds($tokens,$db[0]);if(end($db)!==$dbClose)dn_setup_password_failure('configuration_format');
    $settings=dn_setup_password_entries($tokens,$dbOpen,$dbClose);$literal=var_export($password,true);
    if(isset($settings['pass'])){
        if(count($settings['pass'])!==1||count($settings['pass'][0])!==1)dn_setup_password_failure('configuration_format');
        $pass=$tokens[$settings['pass'][0][0]];if($pass['id']!==T_CONSTANT_ENCAPSED_STRING)dn_setup_password_failure('configuration_format');
        $next=substr($source,0,$pass['start']).$literal.substr($source,$pass['end']);
    }else{
        $newline=str_contains($source,"\r\n")?"\r\n":"\n";$offset=$tokens[$dbOpen]['end'];
        $next=substr($source,0,$offset).$newline."        'pass' => ".$literal.','.substr($source,$offset);
    }
    dn_setup_password_tokens($next);return $next;
}

/** TCP deliberately excludes socket/named-pipe authentication that ignores passwords. */
function dn_setup_password_connect(array $cfg,string $password): array {
    $db=null;
    try{
        $db=mysqli_init();if(!$db)return [null,0];$db->options(MYSQLI_OPT_CONNECT_TIMEOUT,3);
        $host=strtolower((string)($cfg['host']??'127.0.0.1'));if($host==='localhost')$host='127.0.0.1';
        $ok=@$db->real_connect($host,'root',$password,'',(int)($cfg['port']??3306));
        if(!$ok){$code=$db->connect_errno;$db->close();return [null,$code];}
        if(!$db->set_charset('utf8mb4')){$db->close();return [null,0];}
        return [$db,0];
    }catch(Throwable $e){$code=(int)$e->getCode();if($db)try{$db->close();}catch(Throwable){}return [null,$code];}
}
function dn_setup_password_current_account(mysqli $db): string {
    try{$result=$db->query('SELECT CURRENT_USER() AS account_name');$row=$result?$result->fetch_assoc():null;$result?->free();}catch(Throwable){dn_setup_password_failure('account_metadata');}
    $account=(string)($row['account_name']??'');if(!str_starts_with($account,'root@'))dn_setup_password_failure('local_root_required');return substr($account,5);
}
function dn_setup_password_accounts(mysqli $db,bool $maria,string $current): array {
    try{
        $result=$db->query($maria?"SELECT User,Host,Priv FROM mysql.global_priv WHERE User='root'":"SELECT User,Host,plugin,authentication_string FROM mysql.user WHERE User='root'");
        if(!$result)dn_setup_password_failure('account_metadata');$out=[];
        while($row=$result->fetch_assoc()){
            $host=(string)$row['Host'];if(!in_array($host,['localhost','127.0.0.1','::1',$current],true))continue;
            if($host!==''&&!preg_match('/^[a-zA-Z0-9_.:%\\/\-]+$/D',$host))dn_setup_password_failure('account_metadata');
            if($maria){$priv=json_decode((string)$row['Priv'],true,32,JSON_THROW_ON_ERROR);$methods=[['plugin'=>(string)($priv['plugin']??'mysql_native_password'),'authentication_string'=>(string)($priv['authentication_string']??'')]];foreach($priv['auth_or']??[] as $auth)if(isset($auth['plugin']))$methods[]=$auth;}
            else $methods=[['plugin'=>(string)$row['plugin'],'authentication_string'=>(string)($row['authentication_string']??'')]];
            $plugins=array_column($methods,'plugin');$nativeHash='*'.strtoupper(sha1(sha1(dn_setup_password_target(),true)));$alreadyTarget=false;
            foreach($methods as $method)if(($method['plugin']??'')==='mysql_native_password'&&hash_equals($nativeHash,strtoupper((string)($method['authentication_string']??''))))$alreadyTarget=true;
            $passwordPlugins=$maria?['mysql_native_password','mysql_old_password','ed25519']:['mysql_native_password','caching_sha2_password','sha256_password'];
            $out[$host]=['host'=>$host,'supports_password'=>(bool)array_intersect($plugins,$passwordPlugins),'already_target'=>$alreadyTarget,'plugins'=>$plugins];
        }
        $result->free();if(!isset($out[$current]))dn_setup_password_failure('account_metadata');return $out;
    }catch(DnSetupDatabasePasswordException $e){throw $e;}catch(Throwable){dn_setup_password_failure('account_metadata');}
}
/** A salted MySQL hash is never guessed: only a proven login can skip its setter. */
function dn_setup_password_mysql_alias_matches(mysqli $admin,array $cfg,string $host): bool {
    $probe=null;
    try{
        // Stay on the already configured local endpoint; do not probe another interface/server.
        [$probe]=dn_setup_password_connect($cfg,dn_setup_password_target());if(!$probe)return false;
        if(dn_setup_password_current_account($probe)!==$host)return false;
        $a=$admin->query('SELECT @@server_uuid AS server_identity');$b=$probe->query('SELECT @@server_uuid AS server_identity');
        if(!$a||!$b)return false;$left=(string)($a->fetch_assoc()['server_identity']??'');$right=(string)($b->fetch_assoc()['server_identity']??'');$a->free();$b->free();
        return $left!==''&&hash_equals($left,$right);
    }catch(Throwable){return false;}finally{if($probe)try{$probe->close();}catch(Throwable){}}
}
function dn_setup_password_set(mysqli $db,string $host,bool $maria): void {
    // The target is a fixed release credential; the host was restricted to account-name characters.
    // SET PASSWORD preserves existing plugins, including MariaDB's socket OR password chain.
    $value="'".dn_setup_password_target()."'";$sql="SET PASSWORD FOR 'root'@'".$host."' = ".($maria?'PASSWORD('.$value.')':$value);
    try{if(!$db->query($sql))dn_setup_password_failure('password_update_failed');}catch(Throwable){dn_setup_password_failure('password_update_failed');}
}

/**
 * Synchronize existing local root accounts, then return a verified server connection.
 * Caller must enforce local POST + CSRF before invoking and must not log submitted secrets.
 * DnSetupDatabasePasswordException::reason is the only machine-readable failure detail.
 */
function dn_setup_password_sync(array $cfg,string $currentPassword=''): array {
    if(PHP_SAPI!=='cli'&&(!in_array((string)($_SERVER['REMOTE_ADDR']??''),['127.0.0.1','::1'],true)||($_SERVER['REQUEST_METHOD']??'')!=='POST'))dn_setup_password_failure('local_only');
    if(strlen($currentPassword)>4096)dn_setup_password_failure('invalid_password_input');
    $host=strtolower((string)($cfg['host']??'127.0.0.1'));$user=(string)($cfg['user']??'root');$port=(int)($cfg['port']??3306);
    if(!in_array($host,['localhost','127.0.0.1','::1'],true)||$user!=='root'||$port<1||$port>65535)dn_setup_password_failure('local_root_required');
    $directory=dirname(__DIR__).'/config';$config=$directory.'/app.php';$lockPath=$directory.'/.nxt-db-password-lock.php';$lock=null;$stage='';$admin=null;$verified=null;$databaseLock=false;
    try{
        if(is_link($config)||is_link($lockPath)||!is_file($config)||!is_readable($config)||!is_writable($config)||!is_writable($directory))dn_setup_password_failure('configuration_unavailable');
        $lock=@fopen($lockPath,'c+b');if(!$lock)dn_setup_password_failure('configuration_unavailable');@chmod($lockPath,0600);
        $deadline=microtime(true)+5;while(!flock($lock,LOCK_EX|LOCK_NB)){if(microtime(true)>$deadline)dn_setup_password_failure('setup_busy');usleep(50000);}
        if(fstat($lock)['size']===0){fwrite($lock,"<?php http_response_code(404); exit;\n");fflush($lock);}
        $original=@file_get_contents($config);if(!is_string($original)||strlen($original)>1048576)dn_setup_password_failure('configuration_unavailable');
        $replacement=dn_setup_password_config_source($original,dn_setup_password_target());
        $stage=$directory.'/.nxt-db-config-'.bin2hex(random_bytes(16)).'.php';$handle=@fopen($stage,'x+b');if(!$handle)dn_setup_password_failure('configuration_unavailable');
        try{
            @chmod($stage,0600);$written=fwrite($handle,$replacement);if($written!==strlen($replacement)||!fflush($handle))dn_setup_password_failure('configuration_unavailable');
            if(function_exists('fsync')&&!fsync($handle))dn_setup_password_failure('configuration_unavailable');
        }finally{fclose($handle);}
        $candidates=array_values(array_unique([dn_setup_password_target(),(string)($cfg['pass']??''),'',$currentPassword]));$authError=false;$authenticatedWith='';
        foreach($candidates as $candidate){[$connection,$code]=dn_setup_password_connect($cfg,$candidate);if($connection){$admin=$connection;$authenticatedWith=$candidate;break;}if(in_array($code,[1045,1698,1044],true))$authError=true;}
        $currentPassword='';unset($candidates,$candidate);
        if(!$admin)dn_setup_password_failure($authError?'cannot_authenticate':'database_unavailable');
        try{$result=$admin->query("SELECT GET_LOCK('digimon_nxt_local_root_password_sync',5) AS acquired");$row=$result?$result->fetch_assoc():null;$result?->free();$databaseLock=(int)($row['acquired']??0)===1;}catch(Throwable){dn_setup_password_failure('setup_busy');}
        if(!$databaseLock)dn_setup_password_failure('setup_busy');
        $account=dn_setup_password_current_account($admin);$maria=stripos($admin->server_info,'MariaDB')!==false;$accounts=dn_setup_password_accounts($admin,$maria,$account);
        if(!$accounts[$account]['supports_password'])dn_setup_password_failure('unsupported_authentication');
        if($authenticatedWith!==dn_setup_password_target())dn_setup_password_set($admin,$account,$maria);
        $authenticatedWith='';
        [$verified]=dn_setup_password_connect($cfg,dn_setup_password_target());
        if(!$verified||dn_setup_password_current_account($verified)!==$account)dn_setup_password_failure('password_verification_failed');
        // Replace only after authentication succeeds; stage already passed write and parse checks.
        $mode=fileperms($config);if($mode!==false)@chmod($stage,$mode&0777);
        if(@file_get_contents($config)!==$original)dn_setup_password_failure('configuration_commit_failed');
        $renamed=false;for($attempt=0;$attempt<3;$attempt++){if(@rename($stage,$config)){$renamed=true;break;}usleep(100000);}
        if(!$renamed)dn_setup_password_failure('configuration_commit_failed');$stage='';clearstatcache(true,$config);
        if(function_exists('opcache_invalidate'))@opcache_invalidate($config,true);
        if(isset($GLOBALS['pv_config']['db']))$GLOBALS['pv_config']['db']['pass']=dn_setup_password_target();
        $updated=1;$socketOnly=0;
        foreach($accounts as $other){
            if($other['host']===$account)continue;
            if(!$other['supports_password']){$socketOnly++;continue;}
            try{
                $alreadyTarget=$other['already_target'];
                if(!$alreadyTarget&&!$maria&&array_intersect($other['plugins'],['caching_sha2_password','sha256_password']))$alreadyTarget=dn_setup_password_mysql_alias_matches($admin,$cfg,$other['host']);
                if(!$alreadyTarget)dn_setup_password_set($admin,$other['host'],$maria);$updated++;
            }catch(Throwable){dn_setup_password_failure('partial_sync');}
        }
        // Reconnect after every account update as a final proof of the connection used by setup.
        $verified->close();$verified=null;[$verified]=dn_setup_password_connect($cfg,dn_setup_password_target());
        if(!$verified||dn_setup_password_current_account($verified)!==$account)dn_setup_password_failure('password_verification_failed');
        $messages=['XAMPP root password synchronized and verified on a fresh local connection.','Game database settings updated. '.$updated.' existing local root account'.($updated===1?'':'s').' synchronized.'];
        if($socketOnly)$messages[]='Existing socket-only root authentication was preserved for '.$socketOnly.' account'.($socketOnly===1?'':'s').'.';
        $result=['db'=>$verified,'messages'=>$messages];$verified=null;return $result;
    }catch(DnSetupDatabasePasswordException $e){throw $e;}catch(Throwable){dn_setup_password_failure('password_update_failed');}
    finally{
        $currentPassword='';if($verified)try{$verified->close();}catch(Throwable){}
        if($admin){if($databaseLock)try{$admin->query("SELECT RELEASE_LOCK('digimon_nxt_local_root_password_sync')");}catch(Throwable){}try{$admin->close();}catch(Throwable){}}
        if($stage!==''&&is_file($stage))@unlink($stage);
        if(is_resource($lock)){@flock($lock,LOCK_UN);fclose($lock);}
    }
}
