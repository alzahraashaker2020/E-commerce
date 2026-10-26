import { Component,inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { PlatformPublicService } from './platform-public.service';
import { ApiService } from '../../core/services/api.service';
@Component({standalone:true,imports:[CommonModule,FormsModule,RouterLink,TranslatePipe],template:`<main><a routerLink="/platform">{{ 'Platform administration'|t }}</a><h1>{{ 'Platform website content'|t }}</h1><p><a routerLink="/platform/home">{{ 'Preview public website'|t }} ↗</a></p><p role="alert" *ngIf="error">{{ error|t }}</p><p role="status" *ngIf="saved">{{ 'Website content saved.'|t }}</p><form *ngIf="doc" #form="ngForm" (ngSubmit)="save()"><section *ngFor="let f of fields"><h2>{{ f.label|t }}</h2><label>{{ 'English'|t }}<textarea [name]="f.key+'En'" [(ngModel)]="doc[f.key+'En']" [maxlength]="f.max" required rows="3"></textarea></label><label>{{ 'Arabic'|t }}<textarea [name]="f.key+'Ar'" [(ngModel)]="doc[f.key+'Ar']" [maxlength]="f.max" required rows="3" dir="rtl"></textarea></label></section><button class="btn btn-primary" [disabled]="busy||!form.valid">{{ 'Save changes'|t }}</button></form></main>`,styles:[`main{max-width:900px;margin:2rem auto;padding:1rem}section{padding:1.5rem;background:white;border:1px solid #ddd;border-radius:12px;margin:1rem 0}label{display:block;margin:1rem 0}textarea{width:100%;padding:.8rem;box-sizing:border-box}h2{font-size:1.2rem}`]})
export class PlatformWebsiteComponent {
 private api=inject(ApiService);private site=inject(PlatformPublicService);doc:any=null;error='';saved=false;busy=false;fields=[{key:'brand',label:'Platform name',max:100},{key:'headline',label:'Main headline',max:180},{key:'intro',label:'Introduction',max:700},{key:'about',label:'About us',max:1500},{key:'mission',label:'Our mission',max:1000}];
 constructor(){this.api.get('platform/site').subscribe({next:d=>this.doc=d,error:()=>this.error='Unable to load website content.'});}
 save():void{this.busy=true;this.saved=false;this.error='';this.api.put('platform/site',this.doc).subscribe({next:d=>{this.doc=d;this.site.site=d;this.saved=true;this.busy=false;},error:e=>{this.busy=false;this.error=e.error?.message??'Unable to save website content.';}});}
}
