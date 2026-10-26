import { Injectable } from '@angular/core';
import { firstValueFrom, timeout } from 'rxjs';
import { ApiService } from './api.service';

interface DeploymentInfo { mode: 'Saas'|'Standalone'; saasEnabled: boolean; isPlatformHost:boolean; }
interface StoreInfo { features:string[]; id:string; name:string; businessMode:'Commerce'|'Catalog'; commerceEnabled:boolean; operationsEnabled:boolean; businessModeLocked:boolean; }

@Injectable({providedIn:'root'})
export class RuntimeService {
  deployment:DeploymentInfo|null=null;
  store:StoreInfo|null=null;
  error='';
  constructor(private api:ApiService){}
  get isPlatformHost():boolean{return this.deployment?.isPlatformHost===true;}
  hasFeature(name:string):boolean{return this.store?.features?.includes(name)===true;}
  get saasEnabled():boolean{return this.deployment?.saasEnabled===true;}
  get commerceEnabled():boolean{return this.store?.commerceEnabled===true;}
  get operationsEnabled():boolean{return this.store?.operationsEnabled===true;}
  async initialize():Promise<void>{
    try{
      this.deployment=await firstValueFrom(this.api.get<DeploymentInfo>('deployment').pipe(timeout(8000)));
      if(!this.isPlatformHost&&(!location.pathname.startsWith('/platform')||!this.saasEnabled))await this.refreshStore();
    }catch{this.error='The store configuration is temporarily unavailable. Please refresh.';}
  }
  async refreshStore():Promise<void>{this.store=await firstValueFrom(this.api.get<StoreInfo>('store').pipe(timeout(8000)));}
}
