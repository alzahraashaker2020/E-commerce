import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { AccountService } from '../../core/services/account.service';

@Component({standalone:true,imports: [TranslatePipe,CommonModule,FormsModule,RouterLink],template:`
<section class="card" style="padding:2rem;max-width:550px;margin:2rem auto">
  <h1>{{ mode==='forgot-password'?'Forgot password':mode==='reset-password'?'Set a new password':'Verify email' }}</h1>
  <p role="alert" *ngIf="error">{{ (error) | t }}</p><p role="status" *ngIf="message">{{ (message) | t }}</p>
  <form (ngSubmit)="submit()" *ngIf="!done">
    <label *ngIf="mode==='forgot-password'">{{ 'Account email' | t }}<input name="email" type="email" [(ngModel)]="email" required autocomplete="email"></label>
    <ng-container *ngIf="mode==='reset-password'">
      <label>{{ 'New password' | t }}<input name="password" type="password" [(ngModel)]="password" minlength="12" maxlength="128" required autocomplete="new-password"></label>
      <label>{{ 'Repeat new password' | t }}<input name="repeat" type="password" [(ngModel)]="repeat" required autocomplete="new-password"></label>
    </ng-container>
    <p *ngIf="mode!=='forgot-password'&&!token">{{ 'Open the complete private link from your email.' | t }}</p>
    <button class="btn btn-primary" [disabled]="busy || (mode!=='forgot-password'&&!token)">{{ busy?'Please wait…':mode==='verify-email'?'Confirm email':'Continue' }}</button>
  </form>
  <p><a routerLink="/login">{{ 'Back to sign in' | t }}</a></p>
</section>`,styles:[`label{display:block;margin:1rem 0}input{display:block;width:100%;padding:.7rem;margin-top:.5rem}[role=alert]{color:#a22}`]})
export class AccountRecoveryComponent implements OnInit {
  mode='forgot-password';email='';password='';repeat='';token='';busy=false;error='';message='';done=false;
  constructor(private api:ApiService,private account:AccountService){}
  ngOnInit():void{
    this.mode=window.location.pathname.split('/').pop()||'forgot-password';
    this.token=new URLSearchParams(window.location.hash.slice(1)).get('token')||'';
    if(this.token) history.replaceState(history.state,'',window.location.pathname);
  }
  submit():void{
    if(this.busy)return;
    if(this.mode==='reset-password'&&(this.password.length<12||this.password!==this.repeat)){this.error='Use at least 12 characters and matching passwords.';return;}
    this.busy=true;this.error='';
    const payload=this.mode==='forgot-password'?{email:this.email.trim()}:this.mode==='reset-password'?{token:this.token,password:this.password}:{token:this.token};
    this.api.post<{message:string}>('account/'+this.mode,payload).subscribe({next:result=>{if(this.mode==='reset-password')this.account.logout();this.busy=false;this.done=true;this.message=result.message;this.password='';this.repeat='';this.token='';},error:err=>{this.busy=false;this.error=err.status===429?'Too many attempts. Please wait before trying again.':err.error?.message||'Unable to complete this action.';}});
  }
}
