// Lectura y escritura de la base de datos SQLite de TiempoUbicacionApp (Ubicaciones.db)
// con sql.js. Funciona igual en la web (WASM) y en MAUI (BlazorWebView), porque en los
// dos casos el código corre en un navegador. sql.js (~700 KB) se carga solo al usarlo.
window.observApp = window.observApp || {};

window.observApp.locationDb = (function () {
    var BASE = '_content/ObservApp.Shared/lib/sqljs/';
    var TABLE = 'LocationEntry';
    // Columnas en el orden de TiempoUbicacionApp. Los nombres coinciden con los [Column]
    // de su modelo (sqlite-net), por eso algunas van en minúscula.
    var TEXT_COLUMNS = ['fechaActual', 'horaLocal', 'horaUtc', 'desfase', 'tipoHorario', 'Location', 'Latitude', 'Longitude'];
    // Esquema que genera sqlite-net (CreateTable<LocationEntry>) en TiempoUbicacionApp.
    var CREATE_TABLE =
        'create table if not exists "LocationEntry"(\n' +
        '"Id" integer primary key autoincrement not null ,\n' +
        '"Date" bigint ,\n' +
        '"fechaActual" varchar ,\n' +
        '"horaLocal" varchar ,\n' +
        '"horaUtc" varchar ,\n' +
        '"desfase" varchar ,\n' +
        '"tipoHorario" varchar ,\n' +
        '"Location" varchar ,\n' +
        '"Latitude" varchar ,\n' +
        '"Longitude" varchar ,\n' +
        '"Zona" integer )';

    var sqlPromise = null;

    function loadScript(src) {
        return new Promise(function (resolve, reject) {
            var s = document.createElement('script');
            s.src = src;
            s.onload = function () { resolve(); };
            s.onerror = function () { reject(new Error('No se pudo cargar ' + src)); };
            document.head.appendChild(s);
        });
    }

    function getSql() {
        if (!sqlPromise) {
            sqlPromise = (typeof window.initSqlJs === 'function' ? Promise.resolve() : loadScript(BASE + 'sql-wasm-browser.js'))
                .then(function () {
                    return window.initSqlJs({ locateFile: function (file) { return BASE + file; } });
                })
                .catch(function (e) {
                    sqlPromise = null;
                    throw e;
                });
        }
        return sqlPromise;
    }

    function quote(name) {
        return '"' + name.replace(/"/g, '""') + '"';
    }

    // Devuelve { error: null | 'invalid' | 'no_table', rows: [...] }.
    // Date se devuelve como texto porque los ticks de .NET no caben en un número de JS.
    async function read(bytes) {
        var SQL = await getSql();
        var db;
        try {
            db = new SQL.Database(bytes);
            var tables = db.exec("SELECT name FROM sqlite_master WHERE type = 'table' AND lower(name) = lower('" + TABLE + "')");
            if (!tables.length || !tables[0].values.length) {
                return { error: 'no_table', rows: [] };
            }
            var tableName = tables[0].values[0][0];

            var info = db.exec('PRAGMA table_info(' + quote(tableName) + ')');
            var columns = {};
            info[0].values.forEach(function (r) { columns[String(r[1]).toLowerCase()] = String(r[1]); });

            function expr(name, cast) {
                var real = columns[name.toLowerCase()];
                if (!real) return 'NULL';
                return cast ? 'CAST(' + quote(real) + ' AS ' + cast + ')' : quote(real);
            }

            var dateCol = columns['date'];
            var sql = 'SELECT ' +
                expr('Id', 'INTEGER') + ', ' +
                (dateCol ? 'CAST(' + quote(dateCol) + ' AS TEXT), typeof(' + quote(dateCol) + ')' : "NULL, 'null'") + ', ' +
                TEXT_COLUMNS.map(function (c) { return expr(c, 'TEXT'); }).join(', ') + ', ' +
                expr('Zona', 'INTEGER') +
                ' FROM ' + quote(tableName);

            var result = db.exec(sql);
            var rows = result.length ? result[0].values.map(function (v) {
                return {
                    id: v[0] || 0,
                    dateValue: v[1],
                    dateType: v[2],
                    fechaActual: v[3],
                    horaLocal: v[4],
                    horaUtc: v[5],
                    desfase: v[6],
                    tipoHorario: v[7],
                    location: v[8],
                    latitude: v[9],
                    longitude: v[10],
                    zona: v[11] || 0
                };
            }) : [];

            return { error: null, rows: rows };
        } catch (e) {
            console.warn('[locationDb] archivo no válido:', e);
            return { error: 'invalid', rows: [] };
        } finally {
            if (db) db.close();
        }
    }

    // Crea una base de datos nueva con el esquema de TiempoUbicacionApp y devuelve sus bytes.
    async function write(rows) {
        var SQL = await getSql();
        var db = new SQL.Database();
        try {
            db.run(CREATE_TABLE);
            db.run('BEGIN');
            var stmt = db.prepare(
                'INSERT INTO "LocationEntry" ("Id", "Date", "fechaActual", "horaLocal", "horaUtc", "desfase", ' +
                '"tipoHorario", "Location", "Latitude", "Longitude", "Zona") ' +
                'VALUES (?, CAST(? AS INTEGER), ?, ?, ?, ?, ?, ?, ?, ?, ?)');
            try {
                rows.forEach(function (r) {
                    stmt.run([
                        r.id, r.dateValue,
                        r.fechaActual || '', r.horaLocal || '', r.horaUtc || '', r.desfase || '',
                        r.tipoHorario || '', r.location || '', r.latitude || '', r.longitude || '',
                        r.zona || 0
                    ]);
                });
            } finally {
                stmt.free();
            }
            db.run('COMMIT');
            return db.export();
        } finally {
            db.close();
        }
    }

    return { read: read, write: write };
})();

// Descarga de archivos generados en memoria (solo navegador; MAUI usa el FileSaver nativo).
window.observApp.files = window.observApp.files || {};
window.observApp.files.download = function (fileName, contentType, bytes) {
    var blob = new Blob([bytes], { type: contentType || 'application/octet-stream' });
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.style.display = 'none';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
};
