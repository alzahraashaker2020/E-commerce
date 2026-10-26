import { LocalDatePipe } from '../../core/i18n/format.pipe';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
@Component({standalone:true,imports: [LocalDatePipe,TranslatePipe,CommonModule],template:`<h1>{{ 'Store activity' | t }}</h1><p>{{ 'Latest management events for your store. Passwords, request contents and customer details are excluded.' | t }}</p><p *ngIf="error" role="alert">{{ (error) | t }}</p><button class="btn" (click)="load()">{{ 'Refresh' | t }}</button><div style="overflow:auto"><table style="width:100%"><thead><tr><th>{{ 'Time' | t }}</th><th>{{ 'Staff ID' | t }}</th><th>{{ 'Operation' | t }}</th><th>{{ 'Result' | t }}</th><th>{{ 'Support reference' | t }}</th></tr></thead><tbody><tr *ngFor="let e of events"><td>{{ (e.createdAtUtc|localDate:'short') | t }}</td><td>{{ e.actor }}</td><td>{{ (label(e.action,e.method)) | t }}</td><td>{{ (e.status>=500?'Error':e.status>=400?'Rejected':'Completed') | t }}</td><td>{{ e.traceId }}</td></tr></tbody></table></div><p *ngIf="!events.length">{{ 'No management events recorded yet.' | t }}</p>`,styles:[`td,th{text-align:start;padding:.7rem;border-bottom:1px solid #ddd}`]})
export class MerchantActivityComponent{
  events:any[]=[];error='';constructor(private api:ApiService){this.load();}
  load():void{this.api.get<any[]>('merchant/activity').subscribe({next:e=>this.events=e,error:()=>this.error='Unable to load store activity.'});}
  label(action:string,method:string):string{const words=action.replace(/^api\/merchant\//,'').replace(/\{[^}]+\}/g,'').replaceAll('/',' ').trim();const verbs:Record<string,string>={POST:'Create / action',PUT:'Update',PATCH:'Update',DELETE:'Remove'};return words+' — '+(verbs[method]||method);}
}
