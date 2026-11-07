<?php
declare(strict_types=1);
require_once __DIR__.'/includes/ui.php';
require_once __DIR__.'/includes/schema.php';
require_once __DIR__.'/includes/setup_database_password.php';

$remote=(string)($_SERVER['REMOTE_ADDR']??'');
$requestHost=(string)($_SERVER['HTTP_HOST']??'');
if(!in_array($remote,['127.0.0.1','::1'],true)||!preg_match('/\A(?:localhost|127\.0\.0\.1|\[::1\])(?::[0-9]{1,5})?\z/i',$requestHost)){
    nxt_status_page(403,'Local setup only','Open this terminal through localhost on the computer hosting Digimon NXT.');
}
header('Cache-Control: no-store');
$messages=[];$error='';$needsCurrentPassword=false;
$cfg=(array)pv_config('db',[]);
$dbName=(string)($cfg['name']??'digimon_nxt');
if(($_SERVER['REQUEST_METHOD']??'GET')==='POST'){
    $currentPassword='';
    try{
        pv_require_csrf();
        if(isset($_POST['fresh'])===isset($_POST['upgrade']))throw new RuntimeException('Choose Create / Repair Schema or Fresh Rebuild.');
        if($dbName===''||strlen($dbName)>64||preg_match('/[\x00-\x1f]/',$dbName)||in_array(strtolower($dbName),['mysql','information_schema','performance_schema','sys'],true)){
            throw new RuntimeException('Choose a dedicated game database in config/app.php before running setup.');
        }
        if(isset($_POST['current_db_password'])&&!is_string($_POST['current_db_password']))throw new RuntimeException('Enter the current MySQL password as text.');
        $currentPassword=(string)($_POST['current_db_password']??'');
        unset($_POST['current_db_password']);
        if(strlen($currentPassword)>4096)throw new RuntimeException('The current MySQL password is too long.');
        // Only this explicit local, CSRF-checked setup action may change MySQL credentials.
        $sync=dn_setup_password_sync($cfg,$currentPassword);
        $currentPassword='';
        $server=$sync['db'];
        $messages=array_merge($messages,$sync['messages']);
        $cfg=(array)pv_config('db',[]);
        $quotedName='`'.str_replace('`','``',$dbName).'`';
        if(isset($_POST['fresh'])){
            if(!$server->query('DROP DATABASE IF EXISTS '.$quotedName))throw new RuntimeException('The configured game database could not be rebuilt.');
            $messages[]='Removed the previous '.$dbName.' game database.';
        }
        if(!$server->query('CREATE DATABASE IF NOT EXISTS '.$quotedName.' CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci'))throw new RuntimeException('The game database could not be created. Check this MySQL account’s database permissions.');
        if(!$server->select_db($dbName))throw new RuntimeException('The game database could not be selected.');
        $GLOBALS['mysql_connection']=$server;
        $messages=array_merge($messages,dn_apply_schema_migrations($server));
        $messages[]='Digimon NXT database is ready. Restart the tamer task and reopen the world console to use the updated connection.';
    }catch(DnSetupDatabasePasswordException $e){
        $error=$e->getMessage();
        $needsCurrentPassword=$e->reason==='cannot_authenticate';
        pv_log('Setup password synchronization stopped: '.$e->reason);
    }catch(mysqli_sql_exception $e){
        $error='A database operation could not finish. Check the MySQL service and permissions, then use Create / Repair Schema again.';
        pv_log('Setup database operation failed (code '.(int)$e->getCode().').');
    }catch(Throwable $e){
        // Display only controlled validation errors; never raw SQL, credentials or request data.
        $allowed=['The form expired. Please try again.','Choose Create / Repair Schema or Fresh Rebuild.','Choose a dedicated game database in config/app.php before running setup.','Enter the current MySQL password as text.','The current MySQL password is too long.','The configured game database could not be rebuilt.','The game database could not be created. Check this MySQL account’s database permissions.','The game database could not be selected.'];
        $error=in_array($e->getMessage(),$allowed,true)?$e->getMessage():'Setup could not finish. Check the MySQL service and the game folder’s write permissions, then try again.';
        pv_log('Setup stopped: '.(in_array($e->getMessage(),$allowed,true)?$e->getMessage():'unexpected setup error'));
    }finally{
        $currentPassword='';unset($_POST['current_db_password']);
    }
}
$ready=false;$count=0;$tableCount=0;
try{
    $db=pv_db();$ready=pv_table_exists('digimon_species')&&pv_table_exists('members');
    if($ready&&($r=$db->query('SELECT COUNT(*) c FROM digimon_species')))$count=(int)$r->fetch_assoc()['c'];
    if($ready&&($r=$db->query('SELECT COUNT(*) c FROM information_schema.tables WHERE table_schema=DATABASE()')))$tableCount=(int)$r->fetch_assoc()['c'];
}catch(Throwable){}
pv_page_start('Setup','',false);
?>
<main class="nxt-document"><section class="pv-card nxt-document-panel">
<span class="pv-eyebrow">LOCAL INSTALLATION UTILITY</span><h1>Digimon NXT Setup</h1>
<p class="pv-subtle">Both setup buttons automatically synchronize your local XAMPP MySQL password and save the game’s connection settings before installing or repairing the schema. Your custom database name, port and other game settings are kept.</p>
<p class="pv-subtle"><strong>Create / Repair Schema</strong> preserves saved players and synchronizes the species catalog and 1,000 rival tamers. <strong>Fresh Rebuild</strong> deletes and recreates only the configured <code><?=pv_h($dbName)?></code> game database.</p>
<?php if($error):?><div class="pv-flash error" role="alert"><?=pv_h($error)?></div><?php endif;?>
<?php foreach($messages as $m):?><div class="pv-flash success" role="status"><?=pv_h($m)?></div><?php endforeach;?>
<div class="dn-stat-strip nxt-setup-stats">
<div><span>Database</span><strong><?=$ready?'READY':'NOT READY'?></strong></div>
<div><span>Species</span><strong><?=$ready?$count:'—'?></strong></div>
<div><span>Tables</span><strong><?=$ready?$tableCount:'—'?></strong></div>
<div><span>Target DB</span><strong style="font-size:14px"><?=pv_h($dbName)?></strong></div>
<div><span>Version</span><strong style="font-size:14px"><?=pv_h((string)pv_config('asset_version'))?></strong></div>
</div>
<form method="post" style="margin-top:18px" autocomplete="off">
<?=pv_csrf_field()?>
<details style="margin-bottom:18px" <?=$needsCurrentPassword?'open':''?>>
<summary>MySQL already has a different password</summary>
<p class="pv-subtle" id="current-db-password-help">Leave this blank for a standard XAMPP installation. Setup automatically tries the installed settings and a blank password. If your server uses another password that is no longer in the game settings, enter it here once. It is used only for this setup request.</p>
<label for="current-db-password">Current MySQL password</label>
<input id="current-db-password" type="password" name="current_db_password" autocomplete="off" maxlength="4096" aria-describedby="current-db-password-help" style="display:block;max-width:100%;width:360px;margin-top:8px;padding:10px;border:1px solid #385971;border-radius:7px;background:#0e2032;color:#e0eef6">
</details>
<div class="pv-actions">
<button class="pv-button" name="upgrade" value="1">Create / Repair Schema</button>
<button class="pv-button pv-button-secondary" name="fresh" value="1" onclick="return confirm('Delete and rebuild the configured game database? Existing game saves will be removed.')">Fresh Rebuild</button>
<a class="pv-button pv-button-secondary" href="<?=pv_h(pv_url('index.php'))?>">Open Game</a>
</div>
</form>
<?php if($ready):?>
<h2>Keep your rivals active</h2>
<p>Open <code>tools/install_tamer_service_windows.bat</code> from the extracted download once. Its game path must match this installed folder: <code><?=pv_h(__DIR__)?></code>. The background task runs tamers with no players online and starts again when Windows starts.</p>
<p>After installation, Tamer Activity shows <strong>Background worker connected</strong>. Keep MySQL running; the worker reconnects automatically after a database restart.</p>
<?php endif;?>
</section></main><?php pv_page_end();?>
