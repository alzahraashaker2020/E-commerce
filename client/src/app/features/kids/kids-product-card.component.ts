import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Component, Input, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProductDetail, ProductVariant } from '../../core/models/api.models';
import { StorefrontService } from '../../core/services/storefront.service';
import { CartService } from '../../core/services/cart.service';
import { StoreCurrencyPipe } from '../../core/pipes/store-currency.pipe';
import { RuntimeService } from '../../core/services/runtime.service';
import { ImageUrlPipe } from '../../core/pipes/image-url.pipe';
@Component({selector:'app-kids-product-card',standalone:true,imports: [TranslatePipe,CommonModule,RouterLink,ImageUrlPipe,StoreCurrencyPipe],template:`
<article class="product-card"><a class="product-photo" [routerLink]="['/product',product.id]"><img [src]="image | imageUrl" [alt]="(product.name) | t" loading="lazy"><span class="badge sale" *ngIf="discount">−{{ (discount) | t }}%</span><span class="badge" *ngIf="!discount && meta.badge">{{ (meta.badge) | t }}</span></a>
<div class="product-copy"><span class="brand">{{ (product.brand) | t }}</span><a class="product-name" [routerLink]="['/product',product.id]">{{ (product.name) | t }}</a><div class="prices" [class.reduced]="discount"><strong>{{ (price | storeCurrency) | t }}</strong><del *ngIf="discount">{{ (meta.compareAtPrice | storeCurrency) | t }}</del></div><span class="saving" *ngIf="discount">{{ 'You save' | t }} {{ (meta.compareAtPrice-price | storeCurrency) | t }}</span><span class="stock" *ngIf="stock>0 && stock<=5 && product.showStockQuantity">{{ 'Only' | t }} {{ (stock) | t }} {{ 'left in this option' | t }}</span>
<div class="options" *ngIf="product.hasVariants" [attr.aria-label]="'Choose size and color' | t"><button *ngFor="let v of product.variants" [class.selected]="selected?.id===v.id" [disabled]="!v.isActive || v.stockQuantity<1" (click)="selected=v;added=false" [attr.aria-pressed]="selected?.id===v.id">{{ (v.sizeName) | t }}<span *ngIf="hasColors"> · {{ (v.colorName) | t }}</span></button></div>
<button *ngIf="runtime.commerceEnabled" class="add-button" [disabled]="!canBuy" (click)="add()">{{ (added?'Added to bag ✓':canBuy?'Add to bag':'Sold out') | t }}</button></div></article>
`,styles:[`:host{display:block;min-width:0}.product-card{height:100%;border:1px solid #edf0f4;border-radius:15px;overflow:hidden;background:white;display:flex;flex-direction:column;transition:box-shadow .2s,transform .2s}.product-card:hover{box-shadow:0 14px 35px #18365612;transform:translateY(-3px)}.product-photo{display:block;aspect-ratio:1;position:relative;background:#f8f8fa;overflow:hidden}.product-photo img{width:100%;height:100%;object-fit:contain;transition:transform .4s}.product-card:hover img{transform:scale(1.04)}.badge{position:absolute;inset-inline-start:12px;top:12px;border-radius:20px;padding:5px 9px;background:#192737;color:white;font-size:10px;font-weight:800}.badge.sale{background:#e76b70}.product-copy{padding:15px 12px 12px;display:flex;flex:1;flex-direction:column}.brand{font-size:10px;letter-spacing:1px;font-weight:700;color:#8993a3;text-transform:uppercase}.product-name{color:#293b52;font-size:14px;line-height:1.6;margin:8px 0 12px;text-decoration:none;min-height:45px;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden}.prices{display:flex;align-items:center;gap:9px;font-size:14px}.prices strong{font-weight:800}.prices del{color:#9299a4;font-size:12px}.reduced strong{color:#c74837}.saving{color:#17816c;font-size:10px;font-weight:700;margin-top:6px}.stock{font-size:10px;color:#bf5a35;margin-top:5px}.options{display:flex;gap:5px;overflow-x:auto;padding:18px 0 7px;margin-top:auto;scrollbar-width:thin}.options button{flex-shrink:0;border:1px solid #e3e8ef;background:white;border-radius:6px;padding:9px 7px;font-size:10px;font-weight:700;color:#334155;cursor:pointer}.options button.selected{background:#203247;color:white;border-color:#203247}.options button:disabled{text-decoration:line-through;color:#b7bec8;background:linear-gradient(135deg,white 46%,#edf0f4 47%,#edf0f4 53%,white 54%)}.add-button{margin-top:6px;border:0;background:var(--kids-primary,#426fb0);color:white;font-weight:800;padding:13px 3px;border-radius:7px;cursor:pointer;font-size:12px;width:100%}.add-button:disabled{background:#d1d9e3;cursor:not-allowed}`]})
export class KidsProductCardComponent implements OnChanges{
 @Input({required:true})product!:ProductDetail;selected:ProductVariant|undefined;added=false;
 constructor(public storefront:StorefrontService,private cart:CartService,public runtime:RuntimeService){}
 ngOnChanges():void{this.selected=this.product.variants.find(v=>v.isActive&&v.stockQuantity>0)||this.product.variants[0];}
 get meta():any{return this.storefront.metadata(this.product.id);}
 get price():number{return this.product.hasVariants?(this.selected?.price??this.product.basePrice):this.product.basePrice;}
 get stock():number{return this.product.hasVariants?(this.selected?.stockQuantity??0):this.product.stockQuantity;}
 get image():string{return this.selected?.imageUrl||this.product.images[0]?.url||'';}
 get hasColors():boolean{return new Set(this.product.variants.map(v=>v.colorId)).size>1;}
 get canBuy():boolean{return this.product.isActive&&this.stock>0&&(!this.product.hasVariants||!!this.selected?.isActive);}
 get discount():number{return this.meta.compareAtPrice>this.price?Math.round((1-this.price/this.meta.compareAtPrice)*100):0;}
 add():void{if(this.canBuy){this.cart.add(this.product.id,1,this.selected?.id);this.added=true;}}
}
