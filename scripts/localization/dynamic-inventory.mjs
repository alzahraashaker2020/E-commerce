import fs from 'node:fs';import path from 'node:path';import ts from '../../client/node_modules/typescript/lib/typescript.js';
const known=Object.fromEntries(fs.readFileSync('scripts/localization/ar-ui.tsv','utf8').trim().split(/\r?\n/).map(r=>r.split('\t')));const keys=new Set();
function scan(dir){for(const e of fs.readdirSync(dir,{withFileTypes:true})){const p=path.join(dir,e.name);if(e.isDirectory()){if(!['obj','bin','node_modules','i18n'].includes(e.name))scan(p);continue;}
 if(p.endsWith('.ts')){const s=fs.readFileSync(p,'utf8');const sf=ts.createSourceFile(p,s,ts.ScriptTarget.Latest,true);function visit(n){if(ts.isStringLiteral(n)&&/^[A-Z][a-zA-Z]/.test(n.text)&&n.text.length>2&&n.text.length<700&&!/[{}<>/\\#]/.test(n.text)&&(!n.text.includes(':')||n.text.includes(' ')))keys.add(n.text);ts.forEachChild(n,visit);}visit(sf);}
 if(p.endsWith('.cs')){const s=fs.readFileSync(p,'utf8');for(const m of s.matchAll(/(?:Fail|InvalidOperationException)\(\s*"([^"\r\n]+)"/g)){if(m[1].includes(' '))keys.add(m[1]);}}
}}
scan('client/src/app');scan('src/Shop.Api');fs.writeFileSync('src/Shop.Api/obj/ui-dynamic-missing.json',JSON.stringify([...keys].filter(k=>!known[k]).sort(),null,2));console.log('Missing dynamic messages: '+[...keys].filter(k=>!known[k]).length);
