import fs from 'node:fs';import path from 'node:path';
import {parseTemplate} from '../../client/node_modules/@angular/compiler/fesm2022/compiler.mjs';
const dir='client/src/app';
const literal=v=>"'"+v.replaceAll('\\','\\\\').replaceAll("'","\\'").replaceAll('\n','\\n').replaceAll('\r','')+"'";
function transform(template,file){const edits=[];const seen=new Set();
 const add=(start,end,text)=>{const key=start+':'+end;if(seen.has(key))return;seen.add(key);edits.push({start,end,text});};
 const plain=s=>s.replace(/\S[\s\S]*\S|\S/g,key=>/[A-Za-z]{2}/.test(key)?'{{ '+literal(key)+' | t }}':key);
 function visit(nodes){for(const n of nodes??[]){const type=n.constructor.name;
  if(type.startsWith('Text')&&/[A-Za-z]{2}/.test(n.value)){const raw=template.slice(n.sourceSpan.start.offset,n.sourceSpan.end.offset);const before=/^\s*/.exec(raw)[0],after=/\s*$/.exec(raw)[0];add(n.sourceSpan.start.offset,n.sourceSpan.end.offset,before+'{{ '+literal(n.value.trim())+' | t }}'+after);}
  if(type==='BoundText'){const raw=template.slice(n.sourceSpan.start.offset,n.sourceSpan.end.offset);let result='',last=0;for(const m of raw.matchAll(/{{([\s\S]*?)}}/g)){result+=plain(raw.slice(last,m.index));result+='{{ ('+m[1].trim()+') | t }}';last=m.index+m[0].length;}result+=plain(raw.slice(last));add(n.sourceSpan.start.offset,n.sourceSpan.end.offset,result);}
  for(const a of n.attributes??[]){if(['placeholder','title','aria-label','alt'].includes(a.name)&&/[A-Za-z]{2}/.test(a.value)){add(a.sourceSpan.start.offset,a.sourceSpan.end.offset,'[attr.'+a.name+']="'+(literal(a.value)+' | t').replaceAll('"','&quot;')+'"');}}
  for(const a of n.inputs??[]){if(['placeholder','title','aria-label','alt','innerHTML'].includes(a.name)||['aria-label'].includes(a.name.replace('attr.',''))){const raw=a.value?.source;if(raw)add(a.valueSpan.start.offset,a.valueSpan.end.offset,'('+raw+') | t');}}
  visit(n.children);
 }}
 const parsed=parseTemplate(template,file,{preserveWhitespaces:true});if(parsed.errors?.length)throw new Error(file+': '+parsed.errors[0]);visit(parsed.nodes);
 for(const e of edits.sort((a,b)=>b.start-a.start))template=template.slice(0,e.start)+e.text+template.slice(e.end);
 return template;
}
let count=0;
function walk(folder){for(const entry of fs.readdirSync(folder,{withFileTypes:true})){const file=path.join(folder,entry.name);if(entry.isDirectory()){if(entry.name!=='i18n')walk(file);continue;}if(!file.endsWith('.component.ts'))continue;let source=fs.readFileSync(file,'utf8');if(source.includes('import { TranslatePipe }'))continue;const inline=/template\s*:\s*`([\s\S]*?)`/.exec(source);const external=/templateUrl\s*:\s*'([^']+)'/.exec(source);if(!inline&&!external)continue;
 if(inline){const updated=transform(inline[1],file);source=source.slice(0,inline.index)+inline[0].replace(inline[1],updated)+source.slice(inline.index+inline[0].length);}else{const html=path.join(path.dirname(file),external[1]);fs.writeFileSync(html,transform(fs.readFileSync(html,'utf8'),html));}
 const relative=path.relative(path.dirname(file),path.join(dir,'core/i18n/translate.pipe')).replaceAll('\\','/');source="import { TranslatePipe } from '"+(relative.startsWith('.')?relative:'./'+relative)+"';\n"+source;source=source.replace(/imports\s*:\s*\[/,'imports: [TranslatePipe,');fs.writeFileSync(file,source);count++;
}}
walk(dir);console.log('Migrated '+count+' standalone templates');
