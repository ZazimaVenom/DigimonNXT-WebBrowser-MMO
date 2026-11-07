# Automatic XAMPP password setup

On the server PC, open `_setup.php` and click **Create / Repair Schema** or **Fresh Rebuild**. Both actions change the actual local MySQL root password to `QuestionMark123456?`, save that value in the installed `config/app.php`, and then perform the selected schema action. Merely opening the setup page does not change credentials. Open it through `localhost`, `127.0.0.1` or `[::1]` on the server PC; arbitrary hostnames are refused for this password-administration page.

A standard blank-password XAMPP installation is handled automatically. Setup also tries the currently configured password and the requested password, so an already-configured server or a retry works without editing the game configuration. If MySQL has another existing password that is absent from the game settings, expand **MySQL already has a different password**, enter that current password once and click the same button again. The field stays empty after submission; its contents are not added to URLs, application logs or session data.

**Create / Repair Schema** preserves the game saves. **Fresh Rebuild** intentionally deletes and recreates the configured game database after password synchronization. Other database contents are untouched. Existing local root accounts share the requested password; other MySQL users and remote database servers are not reset.

The installed configuration keeps its database name, host, port, base path and other custom values. The update is written as a complete PHP configuration rather than a partial in-place edit. Keep `config/app.php` and its containing directory writable by the local XAMPP Apache service during setup. Restart the existing tamer task and reopen the world console afterward so their next connection reads the updated configuration.

If authentication fails, the installer does not bypass MySQL authentication or start it with grant checking disabled. If a later step fails after the server password has changed, the next setup attempt tries the requested password first and can finish synchronization. Repair can be repeated without selecting Fresh Rebuild.

The small Automatic XAMPP Setup Fix ZIP contains only `_setup.php` and its helper plus instructions. Copy its `public_html` contents into the installed game folder, preserving `config/app.php`. Then use the setup page normally. No separate password utility is required.

## Implementation references

The helper uses the server's supported password statement for an authenticated account and verifies the resulting password with a fresh TCP connection. It retains supported authentication plugins instead of directly editing MySQL privilege tables. These behaviors follow the primary [MariaDB SET PASSWORD reference](https://mariadb.com/docs/server/reference/sql-statements/account-management-sql-statements/set-password) and [MySQL SET PASSWORD reference](https://dev.mysql.com/doc/refman/8.0/en/set-password.html).
