<?php
declare(strict_types=1);
/** Pure configuration/password-boundary checks; no database connections or writes. */
if(PHP_SAPI!=='cli'){http_response_code(404);exit;}
require_once __DIR__.'/../public_html/includes/setup_database_password.php';
set_error_handler(static function(int $n,string $s,string $f,int $l): never {throw new ErrorException($s,0,$n,$f,$l);});
$checks=0;
function sp_assert(bool $ok,string $name): void {global $checks;if(!$ok)throw new RuntimeException('FAIL: '.$name);$checks++;}
function sp_reject(string $source,string $name): void {
    try{dn_setup_password_config_source($source,'target');}catch(DnSetupDatabasePasswordException $e){sp_assert($e->reason==='configuration_format',$name);return;}
    throw new RuntimeException('FAIL: accepted '.$name);
}
function sp_value(string $source): array {return eval(substr($source,5));}
$source= <<<'CONFIG'
<?php
declare(strict_types=1);
// Keep the server's settings and these comments.
return [
    'app_name' => 'Custom server',
    'base_path' => '/my-custom-game',
    'debug' => false,
    'db' => [
        'host' => '127.0.0.1',
        'port' => 3307,
        'name' => 'my_saved_game',
        'user' => 'root',
        'pass' /* retain comment */ => 'old-password', // keep this too
        'charset' => 'utf8mb4',
    ],
    'extra' => ['pass' => 'unrelated-secret', 'result' => strlen('hello')],
];
CONFIG;
$expected=str_replace("'old-password'",var_export(dn_setup_password_target(),true),$source);
$updated=dn_setup_password_config_source($source,dn_setup_password_target());
sp_assert($updated===$expected,'only db.pass literal changes; comments, functions and custom settings stay exact');
sp_assert(dn_setup_password_config_source($updated,dn_setup_password_target())===$updated,'repeated replacement is byte-identical');
$before=sp_value($source);$after=sp_value($updated);$before['db']['pass']=dn_setup_password_target();sp_assert($before===$after,'evaluated unrelated configuration stays identical');
$crlf=str_replace("\n","\r\n",$source);$updatedCrlf=dn_setup_password_config_source($crlf,dn_setup_password_target());sp_assert($updatedCrlf===str_replace("\n","\r\n",$expected),'CRLF preserved exactly');
$missing=str_replace("        'pass' /* retain comment */ => 'old-password', // keep this too\n",'',$source);$inserted=dn_setup_password_config_source($missing,dn_setup_password_target());$expectedMissing=sp_value($missing);$expectedMissing['db']['pass']=dn_setup_password_target();$actualMissing=sp_value($inserted);ksort($expectedMissing['db']);ksort($actualMissing['db']);sp_assert($expectedMissing===$actualMissing,'missing pass inserted without losing custom settings');sp_assert(dn_setup_password_config_source($inserted,dn_setup_password_target())===$inserted,'inserted setting is idempotent');
$long="<?php return array('custom' => 17, 'db' => array('host' => '127.0.0.1', 'pass' => 'old'), 'other' => array('pass' => 'stay'));";
sp_assert(dn_setup_password_config_source($long,'new')===str_replace("'pass' => 'old'","'pass' => 'new'",$long),'array() syntax supported');
$empty="<?php return ['db' => []];";sp_assert(sp_value(dn_setup_password_config_source($empty,'new'))===['db'=>['pass'=>'new']],'empty db array receives pass');
$quoted="a'b\\c\nline";$escaped=dn_setup_password_config_source($source,$quoted);sp_assert(sp_value($escaped)['db']['pass']===$quoted,'quoted, backslash and newline password values remain exact');sp_assert(sp_value($escaped)['extra']['pass']==='unrelated-secret','unrelated pass keys remain unchanged');
$double='<?php return ["db" => ["pass" => "old", "host" => "127.0.0.1"]];';sp_assert(sp_value(dn_setup_password_config_source($double,'new'))['db']['pass']==='new','ordinary double-quoted keys supported');
sp_reject("<?php return ['db'=>['pass'=>'a'], 'db'=>['pass'=>'b']];",'duplicate db key');
sp_reject("<?php return ['db'=>['pass'=>'a', 'pass'=>'b']];",'duplicate pass key');
sp_reject("<?php return ['debug'=>false, 'debug'=>true, 'db'=>['pass'=>'a']];",'duplicate unrelated root key');
sp_reject("<?php return ['db'=>['port'=>3306, 'port'=>3307, 'pass'=>'a']];",'duplicate unrelated db key');
sp_reject('<?php $key="db"; return ["db"=>["pass"=>"a"], $key=>["pass"=>"b"]];','dynamic root key');
sp_reject('<?php $key="pass"; return ["db"=>["pass"=>"a", $key=>"b"]];','dynamic db key');
sp_reject('<?php return ["db"=>["pass"=>"a", "p\u{0061}ss"=>"b"]];','Unicode-escaped pass shadow');
sp_reject('<?php return ["db"=>["pass"=>"a"], "d\x62"=>["pass"=>"b"]];','hex-escaped db shadow');
sp_reject('<?php return ["db"=>["pass"=>"a"], ...["db"=>["pass"=>"b"]]];','unpacked root overrides');
sp_reject('<?php return ["db"=>["pass"=>"a", ...["pass"=>"b"]]];','unpacked db overrides');
sp_reject('<?php return ["db"=>["pass"=>"a"]] + ["extra"=>true];','return expression suffix');
sp_reject('<?php return ["db"=>["pass"=>"a"]]["db"];','return indexing suffix');
sp_reject('<?php return ["db"=>["pass"=>getenv("DB_PASSWORD")]];','computed pass value');
sp_reject('<?php return ["db"=>["pass"=>"a"."b"]];','concatenated pass expression');
sp_reject('<?php return ["db"=>array_merge(["pass"=>"a"], [])];','computed db array');
sp_reject('<?php return ["db"=>["pass"=>"a"]]; return ["db"=>["pass"=>"b"]];','multiple top-level returns');
foreach([
    [['host'=>'192.0.2.1','user'=>'root'],'','local_root_required'],
    [['host'=>'127.0.0.1','user'=>'game_user'],'','local_root_required'],
    [['host'=>'127.0.0.1','user'=>'root','port'=>0],'','local_root_required'],
    [['host'=>'127.0.0.1','user'=>'root'],str_repeat('x',4097),'invalid_password_input'],
] as [$cfg,$password,$reason]){
    try{dn_setup_password_sync($cfg,$password);throw new RuntimeException('FAIL: accepted invalid local boundary');}
    catch(DnSetupDatabasePasswordException $e){sp_assert($e->reason===$reason,'reject invalid host/user/port/input before I/O');sp_assert(!str_contains($e->getMessage(),$password?:'192.0.2.1'),'failure does not expose input');}
}
echo 'SETUP PASSWORD CONFIG: PASS ('.$checks." checks; no database changes or filesystem writes)\n";
