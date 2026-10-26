import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MerchantAuthService } from '../../../core/services/merchant-auth.service';
import { ApiService } from '../../../core/services/api.service';
import { RuntimeService } from '../../../core/services/runtime.service';

/**
 * Merchant shell: a sidebar of management links plus the routed merchant outlet.
 * Rendered only for authenticated merchants (guarded by merchantGuard).
 */
@Component({
  selector: 'app-merchant-shell',
  standalone: true,
  imports: [TranslatePipe,CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './merchant-shell.component.html',
  styleUrl: './merchant-shell.component.css'
})
export class MerchantShellComponent {
  username: string | null;
  sellingPaused = false;
  merchantMenuOpen=false;

  constructor(private auth: MerchantAuthService, private router: Router, api: ApiService, public runtime:RuntimeService) {
    // Assigned in the constructor body so `auth` is initialised first (TS2729).
    this.username = this.auth.getUsername();
    if(runtime.saasEnabled)api.get<{canSell: boolean}>('merchant/subscription').subscribe({next: value => this.sellingPaused = !value.canSell, error: () => {}});
  }

  /** Signs out and returns to the merchant login. */
  logout(): void {
    this.auth.logout();
    this.router.navigate(['/merchant/login']);
  }
}
