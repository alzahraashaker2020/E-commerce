import { Component,inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LanguageSwitchComponent } from '../../core/i18n/language-switch.component';
import { ApiService } from '../../core/services/api.service';
import { PlatformPublicService } from './platform-public.service';
@Component({selector:'app-platform-home',standalone:true,imports:[CommonModule,RouterLink,TranslatePipe,LanguageSwitchComponent],templateUrl:'./platform-home.component.html',styleUrl:'./platform-public.css'})
export class PlatformHomeComponent {
 readonly site=inject(PlatformPublicService);private api=inject(ApiService);plans:any[]=[];products:any[]=[];error='';
 constructor(){this.api.get<any[]>('platform/onboarding/plans').subscribe({next:p=>this.plans=p,error:()=>this.error='Unable to load subscription plans.'});this.api.get<any>('platform/marketplace',{pageSize:4}).subscribe({next:r=>this.products=r.items,error:()=>{}});}
}
