import { Injectable } from '@angular/core';
import { registerLocaleData } from '@angular/common';


@Injectable({providedIn:'root'})
export class LanguageService {
  readonly storageKey='shop_language:'+location.hostname;
  language:'en'|'ar'='en';
  translations:Record<string,string>={};
  private ui:Record<string,string>={};
  private dictionary?:Promise<void>;
  initialize():Promise<void>{return this.arabic?this.load():Promise.resolve();}
  private load():Promise<void>{return this.dictionary??=Promise.all([import('./ar'),import('@angular/common/locales/ar')]).then(([m,locale])=>{registerLocaleData(locale.default,'ar-EG');registerLocaleData(locale.default,'ar');this.ui=m.ARABIC;}).catch(()=>{this.dictionary=undefined;this.language='en';this.applyDirection();});}
  constructor(){try{this.language=localStorage.getItem(this.storageKey)==='ar'?'ar':'en';}catch{}this.applyDirection();}
  get arabic():boolean{return this.language==='ar';}
  get locale():string{return this.arabic?'ar-EG':'en-US';}
  async set(language:'en'|'ar'):Promise<void>{if(language==='ar'){await this.load();if(!Object.keys(this.ui).length)return;}this.language=language;try{localStorage.setItem(this.storageKey,language);}catch{}this.applyDirection();}
  toggle():void{this.set(this.arabic?'en':'ar');}
  private applyDirection():void{document.documentElement.lang=this.language;document.documentElement.dir=this.arabic?'rtl':'ltr';}
  text<T>(value:T):T|string {
    if(!this.arabic||typeof value!=='string')return value;
    const key=value.trim();
    const translated=(Object.hasOwn(this.translations,key)?this.translations[key]:undefined)||(Object.hasOwn(this.ui,key)?this.ui[key]:undefined);
    if(translated)return value.replace(key,translated);
    const lookup=(source:string)=>Object.hasOwn(this.translations,source)?this.translations[source]:Object.hasOwn(this.ui,source)?this.ui[source]:undefined;
    const composite=/^(.*) \(([^()]*)\)$/.exec(key);
    if(composite&&lookup(composite[1]))return lookup(composite[1])+' ('+composite[2].split(', ').map(part=>lookup(part)||part).join('، ')+')';
    const stock=/^(\d+) \(variants\)$/.exec(key);if(stock)return stock[1]+' (مقاسات وألوان)';
    const missingProduct=/^Product (\d+) not found$/.exec(key);if(missingProduct)return 'المنتج '+missingProduct[1]+' غير موجود';
    const missingVariant=/^Variant (\d+) not found for product (\d+)$/.exec(key);if(missingVariant)return 'المقاس واللون '+missingVariant[1]+' غير موجود للمنتج '+missingVariant[2];
    const field=(name:string)=>lookup(({GuestName:'Full name',GuestEmail:'Email',ContactPhone:'Contact phone',AreaId:'Delivery area',AddressLine1:'Address line 1',PostalCode:'Postal code',IdempotencyKey:'Order reference',ProductId:'Product',Quantity:'Quantity'} as Record<string,string>)[name]||name)||name;
    if(key.startsWith('The ')&&key.includes('; '))return key.split('; ').map(part=>this.text(part)).join('؛ ');
    const required=/^The (.+) field is required\.$/.exec(key);if(required)return 'حقل '+field(required[1])+' مطلوب.';
    const email=/^The (.+) field is not a valid e-mail address\.$/.exec(key);if(email)return 'أدخل بريدًا إلكترونيًا صحيحًا.';
    const length=/^The field (.+) must be a string or array type with a maximum length of '(\d+)'\.$/.exec(key);if(length)return 'حقل '+field(length[1])+' يجب ألا يتجاوز '+length[2]+' حرفًا.';

    return value;
  }
}
