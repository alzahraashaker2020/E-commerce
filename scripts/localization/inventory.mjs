import fs from 'node:fs';
import path from 'node:path';
import {parseTemplate} from '../../client/node_modules/@angular/compiler/fesm2022/compiler.mjs';
const root='client/src/app';const keys=new Set();let templates=0;
function walkNodes(nodes){for(const n of nodes??[]){if(n.constructor.name.startsWith('Text')){const key=n.value.trim();if(/[A-Za-z]{2}/.test(key))keys.add(key)}
 if(n.constructor.name==='BoundText'){const source=n.value.source??'';for(const part of source.split(/{{[\s\S]*?}}/g)){if(/[A-Za-z]{2}/.test(part.trim()))keys.add(part.trim())}}
 for(const a of n.attributes??[]){if(['placeholder','title','aria-label','alt'].includes(a.name)&&/[A-Za-z]{2}/.test(a.value))keys.add(a.value)}
 walkNodes(n.children);}}
function walk(dir){for(const e of fs.readdirSync(dir,{withFileTypes:true})){const p=path.join(dir,e.name);if(e.isDirectory())walk(p);else if(e.name.endsWith('.component.html')){templates++;walkNodes(parseTemplate(fs.readFileSync(p,'utf8'),p,{preserveWhitespaces:true}).nodes)}else if(e.name.endsWith('.component.ts')){const s=fs.readFileSync(p,'utf8');const m=/template\s*:\s*`([\s\S]*?)`/.exec(s);if(m){templates++;walkNodes(parseTemplate(m[1],p,{preserveWhitespaces:true}).nodes)}}}}
walk(root);fs.writeFileSync('src/Shop.Api/obj/ui-translation-keys.json',JSON.stringify([...keys].sort(),null,2));console.log({templates,keys:keys.size});
