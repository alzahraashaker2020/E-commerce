import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AccountService } from '../../../core/services/account.service';

/**
 * Signed-in customer account hub: tabbed navigation (profile, orders, reviews)
 * plus the routed account outlet. Guarded by accountGuard.
 */
@Component({
  selector: 'app-account-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './account-shell.component.html',
  styleUrl: './account-shell.component.css'
})
export class AccountShellComponent {
  readonly name$;

  constructor(private account: AccountService, private router: Router) {
    // Assigned in the constructor body so `account` is initialised first (TS2729).
    this.name$ = this.account.name$;
  }

  /** Signs out and returns to the storefront. */
  logout(): void {
    this.account.logout();
    this.router.navigate(['/catalog']);
  }
}
