import fs from 'node:fs';
const rows=fs.readFileSync('scripts/localization/ar-ui.tsv','utf8').trim().split(/\r?\n/);
const map=Object.fromEntries(rows.map(r=>r.split('\t')));
fs.writeFileSync('client/src/app/core/i18n/ar.ts','// Reviewed Arabic UI messages. Protocol codes and API values remain English.\nexport const ARABIC:Record<string,string>='+JSON.stringify(map,null,2)+';\n');
console.log(Object.keys(map).length+' Arabic messages');
