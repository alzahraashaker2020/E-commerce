import { LanguageService } from '../../core/i18n/language.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { StorefrontService } from '../../core/services/storefront.service';
import { ImageUrlPipe } from '../../core/pipes/image-url.pipe';
import { KidsProductCardComponent } from './kids-product-card.component';
@Component({selector:'app-kids-home',standalone:true,imports: [TranslatePipe,CommonModule,RouterLink,ImageUrlPipe,KidsProductCardComponent],template:`
<ng-container *ngIf="storefront.content as c">
<section class="intro"><span class="eyebrow">{{ (c.introEyebrow) | t }}</span><h1>{{ (c.introTitle) | t }}</h1><div class="category-row"><a *ngFor="let tile of c.categories" routerLink="/catalog" [queryParams]="{categoryId:tile.categoryId}"><div><img [src]="tile.imageUrl | imageUrl" [alt]="(tile.label) | t" loading="eager"></div><span>{{ (tile.label) | t }}</span></a></div></section>
<ng-container *ngTemplateOutlet="banner;context:{$implicit:c.banners[0]}"></ng-container>
<section class="age-section" *ngIf="c.ages.length"><h2>{{ (c.agesTitle) | t }}</h2><div class="age-grid"><a *ngFor="let tile of c.ages" routerLink="/catalog" [queryParams]="{categoryId:tile.categoryId}"><img [src]="tile.imageUrl | imageUrl" [alt]="(tile.label) | t" loading="lazy"><span>{{ (tile.label) | t }}</span></a></div></section>
<ng-container *ngFor="let section of c.sections;let i=index"><section class="product-section"><div class="section-head"><div><span class="eyebrow" *ngIf="section.eyebrow">{{ (section.eyebrow) | t }}</span><h2>{{ (section.title) | t }}</h2><p>{{ (section.subtitle) | t }}</p></div><a class="view-all" routerLink="/catalog" [queryParams]="{categoryId:section.categoryId}">{{ 'Shop collection' | t }} <span>↗</span></a></div><div class="shelf-wrap"><button class="shelf-arrow previous" [attr.aria-label]="'Previous products' | t" (click)="shelf.scrollBy({left:language.arabic?700:-700,behavior:'smooth'})">‹</button><div class="product-shelf" #shelf><app-kids-product-card *ngFor="let product of storefront.sectionProducts(section)" [product]="product"></app-kids-product-card></div><button class="shelf-arrow next" [attr.aria-label]="'Next products' | t" (click)="shelf.scrollBy({left:language.arabic?-700:700,behavior:'smooth'})">›</button></div></section><ng-container *ngIf="i===0"><ng-container *ngTemplateOutlet="banner;context:{$implicit:c.banners[1]}"></ng-container></ng-container></ng-container>
<p class="load-error" role="alert" *ngIf="storefront.error">{{ (storefront.error) | t }}</p>
<section class="benefits" *ngIf="c.benefits.length"><article *ngFor="let b of c.benefits;let i=index"><span class="benefit-icon">{{ (['♡','↺','♧','✓','☆','☺'][i]) | t }}</span><div><strong>{{ (b.title) | t }}</strong><p>{{ (b.subtitle) | t }}</p></div></article></section>
<ng-template #banner let-b><a *ngIf="b" class="campaign" routerLink="/catalog" [queryParams]="{categoryId:b.categoryId}" [style.background-color]="b.backgroundColor" [style.color]="b.textColor"><img [src]="b.imageUrl | imageUrl" alt="" loading="lazy"><div class="campaign-copy"><span>{{ (b.eyebrow) | t }}</span><h2>{{ (b.title) | t }}</h2><p>{{ (b.subtitle) | t }}</p><span class="campaign-button" [style.background-color]="b.textColor" [style.color]="b.backgroundColor">{{ (b.buttonLabel) | t }} <b>→</b></span></div><span class="campaign-arrow">↗</span></a></ng-template>
</ng-container>`,styleUrl:'./kids-home.component.css'})
export class KidsHomeComponent{constructor(public storefront:StorefrontService,public language:LanguageService){}}
