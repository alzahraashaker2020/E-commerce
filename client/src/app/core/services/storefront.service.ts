import { Injectable } from '@angular/core';
import { LanguageService } from '../i18n/language.service';
import { RuntimeService } from './runtime.service';
import { ApiService } from './api.service';
import { ProductDetail } from '../models/api.models';
export interface StorefrontLink{label:string;subtitle?:string;categoryId:number|null;imageUrl?:string;}
export interface StorefrontDocument{includeInMarketplace:boolean;version:number;businessMode:string;translations:Record<string,string>;theme:string;brandName:string;brandSubtitle:string;announcement:string;shippingText:string;primaryColor:string;introEyebrow:string;introTitle:string;agesTitle:string;footerText:string;supportEmail:string;supportPhone:string;newsletterEnabled:boolean;newsletterTitle:string;newsletterSubtitle:string;navigation:StorefrontLink[];promotions:StorefrontLink[];categories:StorefrontLink[];ages:StorefrontLink[];banners:any[];sections:any[];benefits:any[];merchandising:any[];}
@Injectable({providedIn:'root'})
export class StorefrontService{
  content:StorefrontDocument|null=null;loaded=false;products:ProductDetail[]=[];error='';
  constructor(private api:ApiService,runtime:RuntimeService,private language:LanguageService){if(!runtime.isPlatformHost&&(!location.pathname.startsWith('/platform')||!runtime.saasEnabled))this.reload();else this.loaded=true;}
  get kids():boolean{return this.content?.theme==='kids';}
  reload():void{this.api.get<StorefrontDocument>('storefront').subscribe({next:c=>{this.content=c;this.language.translations=c.translations||{};this.loaded=true;const ids=[...new Set(c.sections.flatMap((s:any)=>s.productIds as number[]))];if(ids.length)this.api.get<any>('storefront/products',{ids:ids.join(','),pageSize:100}).subscribe({next:r=>this.products=r.items,error:()=>this.error='Could not load the collection. Please refresh.'});},error:()=>{this.loaded=true;this.error='Storefront content is temporarily unavailable.';}});}
  metadata(id:number):any{return this.content?.merchandising.find(m=>m.productId===id)||{};}
  sectionProducts(section:any):ProductDetail[]{return section.productIds.map((id:number)=>this.products.find(p=>p.id===id)).filter(Boolean);}
}
