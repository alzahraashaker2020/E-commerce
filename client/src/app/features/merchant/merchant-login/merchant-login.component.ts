import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MerchantAuthService } from '../../../core/services/merchant-auth.service';

/**
 * Merchant login page. Authenticates against api/auth/login and, on success,
 * navigates to the intended merchant destination (returnUrl) or the dashboard.
 */
@Component({
  selector: 'app-merchant-login',
  standalone: true,
  imports: [TranslatePipe,CommonModule, FormsModule, RouterLink],
  templateUrl: './merchant-login.component.html',
  styleUrl: './merchant-login.component.css'
})
export class MerchantLoginComponent {
  username = '';
  password = '';
  submitting = false;
  error: string | null = null;

  constructor(private auth: MerchantAuthService, private router: Router, private route: ActivatedRoute) {}

  /** Submits the credentials and redirects on success. */
  login(): void {
    this.submitting = true;
    this.error = null;
    this.auth.login({ username: this.username.trim(), password: this.password }).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/merchant/dashboard';
        this.router.navigateByUrl(/^\/merchant(?:\/|$)/.test(returnUrl) && !returnUrl.includes('..') ? returnUrl : '/merchant/dashboard');
      },
      error: () => {
        this.submitting = false;
        this.error = 'Invalid credentials.';
      }
    });
  }
}
