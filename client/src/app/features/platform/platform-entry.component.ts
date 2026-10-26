import { Component,inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RuntimeService } from '../../core/services/runtime.service';
import { PlatformHomeComponent } from './platform-home.component';
import { StorefrontPageComponent } from '../kids/storefront-page.component';
@Component({standalone:true,imports:[CommonModule,PlatformHomeComponent,StorefrontPageComponent],template:`<app-platform-home *ngIf="runtime.isPlatformHost;else store"></app-platform-home><ng-template #store><app-storefront-page></app-storefront-page></ng-template>`})
export class PlatformEntryComponent {readonly runtime=inject(RuntimeService);}
