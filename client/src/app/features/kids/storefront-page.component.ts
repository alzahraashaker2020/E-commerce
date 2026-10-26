import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { StorefrontService } from '../../core/services/storefront.service';
import { CatalogComponent } from '../catalog/catalog.component';
import { KidsHomeComponent } from './kids-home.component';
import { KidsCatalogComponent } from './kids-catalog.component';
@Component({selector:'app-storefront-page',standalone:true,imports: [TranslatePipe,CommonModule,CatalogComponent,KidsHomeComponent,KidsCatalogComponent],template:`<ng-container *ngIf="storefront.loaded"><ng-container *ngIf="storefront.kids;else classic"><app-kids-home *ngIf="router.url.split('?')[0]==='/';else collection"></app-kids-home><ng-template #collection><app-kids-catalog></app-kids-catalog></ng-template></ng-container><ng-template #classic><app-catalog></app-catalog></ng-template></ng-container>`})
export class StorefrontPageComponent{constructor(public storefront:StorefrontService,public router:Router){}}
