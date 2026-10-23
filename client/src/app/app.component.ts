import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { CartService } from './core/services/cart.service';
import { AccountService } from './core/services/account.service';
import { SettingsService } from './core/services/settings.service';

/**
 * Root shell: a storefront header with primary navigation, a live cart badge,
 * and an account/sign-in link, plus the routed outlet. The admin and account
 * areas render their own sub-shells inside this outlet.
 */
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  /** Reactive cart quantity shown as a badge in the header. */
  readonly cartCount$;
  /** Reactive customer signed-in flag for the account/sign-in link. */
  readonly loggedIn$;

  constructor(
    private cart: CartService,
    private account: AccountService,
    private settings: SettingsService
  ) {
    // Assigned in the constructor body so the injected services are
    // initialised first (TS2729 — no field-initializer use before ctor).
    this.cartCount$ = this.cart.count$;
    this.loggedIn$ = this.account.loggedIn$;
    // Load the store currency settings once at startup.
    this.settings.load().subscribe();
  }
}
