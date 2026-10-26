import { ManagementShellComponent } from './shared/management/management-shell.component';
import { LanguageSwitchComponent } from './core/i18n/language-switch.component';
import { TranslatePipe } from './core/i18n/translate.pipe';
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { CartService } from './core/services/cart.service';
import { AccountService } from './core/services/account.service';
import { SettingsService } from './core/services/settings.service';
import { ApiService } from './core/services/api.service';
import { StorefrontService } from './core/services/storefront.service';
import { KidsShellComponent } from './features/kids/kids-shell.component';
import { RuntimeService } from './core/services/runtime.service';

/**
 * Root shell: a storefront header with primary navigation, a live cart badge,
 * and an account/sign-in link, plus the routed outlet. The merchant and account
 * areas render their own sub-shells inside this outlet.
 */
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [ManagementShellComponent,LanguageSwitchComponent,TranslatePipe,CommonModule, RouterOutlet, RouterLink, RouterLinkActive, KidsShellComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  get useKidsShell():boolean{return this.storefront.kids&&!this.isPlatform&&!this.isAccessPage&&!this.router.url.startsWith('/merchant')&&!this.router.url.startsWith('/admin');}
  storeName = 'Shop';
  get isPlatform(): boolean { return this.runtime.isPlatformHost||this.router.url.startsWith('/platform'); }
  get isPlatformManagement():boolean{return this.router.url.startsWith('/platform')&&!/^\/platform\/(home|shop)(?:[/?#]|$)/.test(this.router.url);}
  get isManagementPage():boolean{return this.isPlatformManagement||(/^\/(merchant|admin)(?:[/?#]|$)/.test(this.router.url)&&!this.isAccessPage);}
  get isAccessPage():boolean{return /^\/(register(?:[/?#]|$)|login(?:[/?#]|$)|merchant\/(register|login)(?:[/?#]|$))/.test(this.router.url);}
  get isPublicPlatform():boolean{return this.runtime.isPlatformHost&&this.router.url==='/'||/^\/platform\/(home|shop)/.test(this.router.url);}
  /** Reactive cart quantity shown as a badge in the header. */
  readonly cartCount$;
  /** Reactive customer signed-in flag for the account/sign-in link. */
  readonly loggedIn$;

  constructor(
    private cart: CartService,
    private account: AccountService,
    private settings: SettingsService,
    private api: ApiService,
    public storefront: StorefrontService,
    private router: Router,
    public runtime: RuntimeService
  ) {
    // Assigned in the constructor body so the injected services are
    // initialised first (TS2729 — no field-initializer use before ctor).
    this.cartCount$ = this.cart.count$;
    this.loggedIn$ = this.account.loggedIn$;
    // Load the store currency settings once at startup.
    if (!this.runtime.isPlatformHost&&!window.location.pathname.startsWith('/platform')) {
      this.settings.load().subscribe({ error: () => {} });
      this.api.get<{name: string}>('store').subscribe({ next: store => this.storeName = store.name, error: () => {} });
    }
  }
}
