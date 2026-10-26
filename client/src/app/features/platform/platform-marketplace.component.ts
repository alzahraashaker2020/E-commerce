import { Component,inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LanguageSwitchComponent } from '../../core/i18n/language-switch.component';
import { ApiService } from '../../core/services/api.service';
import { PlatformPublicService } from './platform-public.service';
@Component({standalone:true,imports:[CommonModule,FormsModule,RouterLink,TranslatePipe,LanguageSwitchComponent],styleUrls:['./platform-public.css','./platform-marketplace.css'],template:`<main class="platform-public">
<header class="public-nav"><a class="logo" routerLink="/platform/home"><span dir="ltr">m.</span>{{ site.text('brand')||('Matjark'|t) }}</a><nav><a routerLink="/platform/home">{{ 'About us'|t }}</a><a routerLink="/platform/home" fragment="plans">{{ 'Plans & pricing'|t }}</a></nav><div class="nav-end"><app-language-switch></app-language-switch><a class="button small dark" routerLink="/merchant/register">{{ 'Start your store'|t }}</a></div></header>
<section class="section market-intro"><p class="eyebrow">{{ 'THE MATJARK MARKETPLACE'|t }}</p><h1>{{ 'Discover your next good find.'|t }}</h1><p>{{ 'Public products from independent merchants. Orders are placed directly in each merchant store.'|t }}</p>
<form class="market-search" (ngSubmit)="load(1)"><input name="search" [(ngModel)]="q" [placeholder]="'Search all stores'|t" [attr.aria-label]="'Search all stores'|t" maxlength="120"><button class="button dark" [disabled]="loading">{{ 'Search'|t }}</button></form>
<p role="alert" *ngIf="error">{{ error|t }}</p><p *ngIf="partial">{{ 'Some stores are temporarily unavailable. Results show the stores available now.'|t }}</p><p *ngIf="!loading">{{ total }} {{ 'Products'|t }}</p>
<div class="market-grid"><a class="market-product" *ngFor="let p of products" [href]="site.storeUrl(p.host,'/product/'+p.productId)"><div class="product-photo"><img loading="lazy" *ngIf="p.imageUrl;else noPhoto" [src]="site.image(p.host,p.imageUrl)" [alt]="site.language.arabic?p.nameAr:p.name"><ng-template #noPhoto><span>✦</span></ng-template><span class="stock-pill" *ngIf="!p.available">{{ 'Out of stock'|t }}</span></div><p class="merchant-label">{{ p.storeName }}</p><h3>{{ site.language.arabic?p.nameAr:p.name }}</h3><strong>{{ p.price|currency:p.currency:'symbol':'1.0-2':site.language.locale }}</strong><span class="visit">{{ 'View in store'|t }} ↗</span></a></div>
<p *ngIf="!products.length&&!loading">{{ 'No matching products. Try another search.'|t }}</p><p *ngIf="loading" role="status">{{ 'Loading…'|t }}</p><div class="pagination"><button class="button" (click)="load(page-1)" [disabled]="loading||page===1">{{ 'Previous'|t }}</button><span>{{ page }} / {{ pages }}</span><button class="button dark" (click)="load(page+1)" [disabled]="loading||page>=pages">{{ 'Next'|t }}</button></div></section></main>`})
export class PlatformMarketplaceComponent {
 readonly site=inject(PlatformPublicService);private api=inject(ApiService);products:any[]=[];q='';page=1;total=0;loading=false;partial=false;error='';
 get pages():number{return Math.max(1,Math.ceil(this.total/24));}
 constructor(){this.load(1);}
 load(page:number):void{this.loading=true;this.error='';this.api.get<any>('platform/marketplace',{q:this.q,page,pageSize:24}).subscribe({next:r=>{this.products=r.items;this.total=r.totalCount;this.page=r.page;this.partial=r.partial;this.loading=false;},error:()=>{this.loading=false;this.error='Unable to load marketplace products.';}});}
}
