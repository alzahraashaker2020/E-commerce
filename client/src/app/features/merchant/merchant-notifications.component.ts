import { LocalDatePipe } from '../../core/i18n/format.pipe';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';

@Component({standalone:true,imports: [LocalDatePipe,TranslatePipe,CommonModule,FormsModule],template:`
<h1>{{ 'Email notifications' | t }}</h1><p *ngIf="error" role="alert">{{ (error) | t }}</p><p *ngIf="saved" role="status">{{ 'Notification preferences saved.' | t }}</p>
<form *ngIf="settings" class="card" style="padding:1.5rem" (ngSubmit)="save()">
  <p>{{ 'Delivery:' | t }} <strong>{{ (settings.delivery) | t }}</strong></p><p *ngIf="settings.from">{{ 'Sender:' | t }} {{ (settings.from) | t }}</p>
  <p *ngIf="!settings.ready">{{ 'A Gmail sender must be connected by the platform before real emails can be delivered. Saving preferences does not connect Gmail.' | t }}</p>
  <label>{{ 'Merchant notification email' | t }}<input type="email" name="merchantEmail" [(ngModel)]="settings.merchantEmail" maxlength="200"></label>
  <label><input type="checkbox" name="customerOrders" [(ngModel)]="settings.customerOrdersEnabled"> {{ 'Customer order confirmation' | t }}</label>
  <label><input type="checkbox" name="customerStatus" [(ngModel)]="settings.customerStatusEnabled"> {{ 'Customer order status changes' | t }}</label>
  <label><input type="checkbox" name="merchantOrders" [(ngModel)]="settings.merchantOrdersEnabled"> {{ 'Notify merchant of new orders' | t }}</label>
  <p>{{ 'Verification and password reset messages remain enabled independently of these order preferences.' | t }}</p>
  <button class="btn btn-primary" [disabled]="busy">{{ 'Save preferences' | t }}</button>
</form>
<h2>{{ 'Message history' | t }}</h2><p>{{ 'DevelopmentOnly means a local test copy, not an email delivered to an inbox. Sent means accepted by the SMTP server. Delivery may repeat after an interrupted attempt.' | t }}</p>
<button class="btn" (click)="loadMessages()">{{ 'Refresh history' | t }}</button>
<div style="overflow:auto"><table><thead><tr><th>{{ 'Recipient' | t }}</th><th>{{ 'Subject' | t }}</th><th>{{ 'Status' | t }}</th><th>{{ 'Attempts in cycle' | t }}</th><th>{{ 'Created' | t }}</th><th>{{ 'Actions' | t }}</th></tr></thead><tbody>
<tr *ngFor="let m of messages"><td>{{ m.recipient }}</td><td>{{ (m.subject) | t }}</td><td>{{ (m.state) | t }}<small *ngIf="m.lastError"> — {{ (m.lastError) | t }}</small></td><td>{{ (m.attempts) | t }}</td><td>{{ (m.createdAtUtc |localDate:'short') | t }}</td><td><button (click)="history(m.id)">{{ 'Attempts' | t }}</button> <button *ngIf="m.state==='Failed'" [disabled]="busy" (click)="retry(m.id)">{{ 'Retry' | t }}</button></td></tr>
</tbody></table></div><p *ngIf="!messages.length">{{ 'No messages recorded yet.' | t }}</p>
<ul><li *ngFor="let a of attempts">{{ (a.attemptedAtUtc |localDate:'short') | t }} — {{ (a.outcome) | t }}</li></ul>
`,styles:[`label{display:block;margin:1rem 0}input[type=email]{display:block;padding:.7rem;width:100%;max-width:450px}td,th{text-align:start;padding:.6rem;border-bottom:1px solid #ddd}table{width:100%}`]})
export class MerchantNotificationsComponent{
  settings:any;error='';saved=false;busy=false;
  messages:any[]=[];attempts:any[]=[];
  constructor(private api:ApiService){api.get('merchant/notifications').subscribe({next:s=>this.settings=s,error:()=>this.error='Unable to load notification settings.'});this.loadMessages();}
  loadMessages():void{this.api.get<any[]>('merchant/notifications/messages').subscribe({next:m=>this.messages=m,error:()=>this.error='Unable to load message history.'});}
  history(id:string):void{this.api.get<any[]>('merchant/notifications/messages/'+id+'/attempts').subscribe({next:a=>this.attempts=a,error:()=>this.error='Unable to load attempts.'});}
  retry(id:string):void{this.busy=true;this.error='';this.api.post('merchant/notifications/messages/'+id+'/retry',{}).subscribe({next:()=>{this.busy=false;this.loadMessages();},error:e=>{this.busy=false;this.error=e.error?.message||'Unable to retry message.'}});}
  save():void{this.busy=true;this.saved=false;this.error='';this.api.put('merchant/notifications',this.settings).subscribe({next:s=>{this.settings=s;this.busy=false;this.saved=true;},error:err=>{this.busy=false;this.error=err.error?.message||'Could not save preferences.';}});}
}
