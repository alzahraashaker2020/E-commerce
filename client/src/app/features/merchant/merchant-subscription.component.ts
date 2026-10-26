import { FEATURE_LABELS } from '../platform/platform-public.service';
import { LocalDatePipe } from '../../core/i18n/format.pipe';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';

@Component({standalone:true,imports: [LocalDatePipe,TranslatePipe,CommonModule],template:`
  <h1>{{ 'My store subscription' | t }}</h1>
  <p role="alert" *ngIf="error">{{ (error) | t }}</p>
  <section class="card" *ngIf="data" style="padding:1.5rem">
    <h2>{{ (data.subscription?.planName || 'No plan assigned') | t }}</h2>
    <p>{{ 'Status:' | t }} <strong>{{ (data.status) | t }}</strong></p>
    <ng-container *ngIf="data.subscription as s">
      <p>{{ 'Plan price:' | t }} {{ (s.priceMinor / 100 | currency:s.currency) | t }} / {{ (s.periodMonths) | t }} {{ 'month(s)' | t }}</p>
      <p>{{ 'Valid until:' | t }} {{ (s.endsAtUtc |localDate:'medium') | t }}</p>
      <p>{{ 'Products:' | t }} {{ (data.productCount) | t }} / {{ (s.maxProducts ?? 'Unlimited') | t }}</p>
      <h3>{{ 'Included features'|t }}</h3><p>{{ 'Products, inventory and order management'|t }}</p><ul><li *ngFor="let f of features()">{{ featureLabels[f]|t }}</li></ul>
    </ng-container>
    <p *ngIf="!data.canSell">{{ 'New orders and management changes are paused. Your existing data is retained. Contact the platform to activate or renew your subscription.' | t }}</p>
    <p *ngIf="data.status==='Legacy'">{{ 'Your existing store continues under its previous access. Contact the platform to select a plan.' | t }}</p>
    <p>{{ 'Activation, changes and renewal are handled by the platform. No automatic payments are collected.' | t }}</p>
  </section>`})
export class MerchantSubscriptionComponent {
  data:any; error='';featureLabels=FEATURE_LABELS;
  features():string[]{try{return JSON.parse(this.data?.subscription?.featuresJson||'[]');}catch{return [];}} 
  constructor(api:ApiService){api.get('merchant/subscription').subscribe({next: result=>this.data=result,error:()=>this.error='Unable to load your subscription.'});}
}
