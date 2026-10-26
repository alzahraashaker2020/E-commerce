import { Injectable,inject } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { LanguageService } from '../../core/i18n/language.service';
import { environment } from '../../../environments/environment';
export const FEATURE_LABELS:Record<string,string>={Discounts:'Discount codes',Newsletter:'Newsletter signups',StorefrontDesign:'Storefront customization',Marketplace:'Marketplace visibility'};
@Injectable({providedIn:'root'})
export class PlatformPublicService {
 private api=inject(ApiService);readonly language=inject(LanguageService);site:any=null;
 constructor(){this.reload();}
 reload():void{this.api.get('platform/site').subscribe({next:s=>this.site=s,error:()=>{}});}
 text(field:string):string{return this.site?.[field+(this.language.arabic?'Ar':'En')]||'';}
 storeUrl(host:string,path:string):string{const url=new URL(location.origin);url.hostname=host;url.pathname=path;url.search='';return url.href;}
 image(host:string,path:string|null):string{if(!path)return '';if(/^https?:\/\//.test(path))return path;if(!path.startsWith('/uploads/'))return '';const url=new URL(environment.apiBaseUrl,location.origin);url.hostname=host;url.pathname=path;url.search='';return url.href;}
 features(json:string):string[]{try{return JSON.parse(json)||[];}catch{return [];}}
 featureLabel(key:string):string{return FEATURE_LABELS[key]||key;}
}
