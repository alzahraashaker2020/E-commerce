import { Component,Input,inject,DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router,RouterLink,RouterLinkActive,NavigationEnd } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LanguageSwitchComponent } from '../../core/i18n/language-switch.component';
import { RuntimeService } from '../../core/services/runtime.service';
import { MerchantAuthService } from '../../core/services/merchant-auth.service';
@Component({selector:'app-management-shell',standalone:true,imports:[CommonModule,RouterLink,RouterLinkActive,TranslatePipe,LanguageSwitchComponent],template:`
<div class="management-theme" [class.platform-workspace]="platform">
 <header class="management-header">
  <a class="workspace-logo" [routerLink]="platform?'/platform':'/merchant/dashboard'"><span dir="ltr">m.</span><div>{{ 'Matjark'|t }}<small>{{ (platform?'Platform workspace':'Merchant workspace')|t }}<ng-container *ngIf="!platform&&runtime.store"> · {{ runtime.store.name }}</ng-container></small></div></a>
  <div class="workspace-tools"><a class="preview-link" [routerLink]="platform?'/platform/home':'/'">{{ (platform?'View public website':'View my store')|t }} ↗</a><app-language-switch></app-language-switch><button type="button" class="workspace-logout" *ngIf="signedIn" (click)="logout()">{{ 'Sign out'|t }}</button></div>
 </header>
 <div class="workspace-layout" [class.with-navigation]="platform&&signedIn">
  <button #menuButton type="button" class="workspace-menu" *ngIf="platform&&signedIn" (click)="menuOpen=!menuOpen" [attr.aria-expanded]="menuOpen" aria-controls="platform-workspace-nav">☰ {{ 'Platform navigation'|t }}</button>
  <aside id="platform-workspace-nav" class="workspace-sidebar" *ngIf="platform&&signedIn" [class.is-open]="menuOpen">
   <p class="workspace-caption">{{ 'YOUR WORKSPACE'|t }}</p>
   <nav [attr.aria-label]="'Platform navigation'|t" (click)="menuOpen=false"><a *ngFor="let item of navigation" [routerLink]="item.path" routerLinkActive="active" [routerLinkActiveOptions]="{exact:item.exact}" ariaCurrentWhenActive="page"><span aria-hidden="true">{{ item.icon }}</span>{{ item.label|t }}</a></nav>
   <div class="workspace-note"><span aria-hidden="true">✦</span><strong>{{ 'Built around your business'|t }}</strong><p>{{ 'One place to manage the bigger picture.'|t }}</p></div>
  </aside>
  <main class="management-content" [class.merchant-workspace-content]="!platform"><ng-content></ng-content></main>
 </div>
 <footer class="workspace-footer">{{ 'Matjark'|t }} · {{ (platform?'Platform administration':'Store management')|t }}</footer>
</div>`,styles:[`:host{display:block}`]})
export class ManagementShellComponent {
 @Input() platform=false;
 readonly runtime=inject(RuntimeService);private merchant=inject(MerchantAuthService);private router=inject(Router);menuOpen=false;
 navigation=[{path:'/platform',label:'Overview & stores',icon:'⊞',exact:true},{path:'/platform/applications',label:'Merchant applications',icon:'◇',exact:false},{path:'/platform/plans',label:'Plans & subscriptions',icon:'▤',exact:false},{path:'/platform/website',label:'Platform website content',icon:'✎',exact:false},{path:'/platform/operations',label:'Backups and operations',icon:'↺',exact:false}];
 constructor(){this.router.events.pipe(takeUntilDestroyed(inject(DestroyRef))).subscribe(e=>{if(e instanceof NavigationEnd)this.menuOpen=false;});}
 get signedIn():boolean{if(!this.platform)return this.merchant.isLoggedIn();try{const p=JSON.parse(atob((localStorage.getItem('shop_platform_token')||'').split('.')[1].replace(/-/g,'+').replace(/_/g,'/')));return p.exp*1000>Date.now();}catch{return false;}}
 logout():void{if(this.platform){localStorage.removeItem('shop_platform_token');this.router.navigateByUrl('/platform').then(()=>location.reload());}else{this.merchant.logout();this.router.navigateByUrl('/merchant/login');}}
}
