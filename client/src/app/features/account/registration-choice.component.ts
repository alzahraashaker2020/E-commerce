import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { RuntimeService } from '../../core/services/runtime.service';

@Component({
  standalone:true,
  imports:[CommonModule,RouterLink,TranslatePipe],
  template:`<main class="registration">
    <h1>{{ (merchant ? 'Merchant account' : 'Choose your account') | t }}</h1>
    <p>{{ 'Customers and merchants have separate accounts and portals.' | t }}</p>
    <section class="choices" *ngIf="!merchant; else setup">
      <article class="card"><h2>{{ 'Customer account' | t }}</h2>
        <p>{{ (runtime.isPlatformHost?'Browse all stores and create a customer account in the store you shop from.':'Shop, track your own orders, manage returns and review purchases.') | t }}</p>
        <a class="btn btn-primary" [routerLink]="runtime.isPlatformHost?'/platform/shop':'/register/customer'">{{ (runtime.isPlatformHost?'Shop as a customer':'Create customer account') | t }}</a>
        <a routerLink="/login">{{ 'Customer sign in' | t }}</a>
      </article>
      <article class="card"><h2>{{ 'Merchant account' | t }}</h2>
        <p>{{ 'Manage your store, products, inventory and customer orders.' | t }}</p>
        <a class="btn btn-primary" routerLink="/merchant/login">{{ 'Merchant sign in' | t }}</a>
        <a routerLink="/merchant/register">{{ 'Submit merchant application' | t }}</a>
      </article>
    </section>
    <ng-template #setup><article class="card setup">
      <h2>{{ (runtime.saasEnabled ? 'A separate store for every merchant' : 'Store owner access') | t }}</h2>
      <p>{{ (runtime.saasEnabled ? 'The platform owner creates your store and merchant credentials with a separate database. Contact the platform owner to set up your store, then sign in at your own store address.' : 'The store owner account is provided when this standalone store is installed. Contact your system administrator for access.') | t }}</p>
      <p>{{ 'Customer registration never grants access to store management.' | t }}</p>
      <a class="btn btn-primary" routerLink="/merchant/login">{{ 'Merchant sign in' | t }}</a>
      <a routerLink="/register">{{ 'Choose your account' | t }}</a>
    </article></ng-template>
  </main>`,
  styles:[`.registration{max-width:900px;margin:2rem auto;padding:1rem}.choices{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:1.25rem;margin-top:1.5rem}article{padding:1.5rem;display:flex;flex-direction:column;gap:1rem}h1{font-size:1.8rem}h2{font-size:1.3rem}.setup{margin-top:1.5rem;max-width:650px}.btn{text-align:center}@media(max-width:600px){.choices{grid-template-columns:1fr}}`]
})
export class RegistrationChoiceComponent {
  readonly runtime=inject(RuntimeService);
  readonly merchant=inject(ActivatedRoute).snapshot.routeConfig?.path==='merchant/register';
}
