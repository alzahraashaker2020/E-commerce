import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AccountService } from '../../../core/services/account.service';

/**
 * Customer login page: local email/password login plus Google/Facebook social
 * login buttons. On success, navigates to the intended destination (returnUrl)
 * or the account area.
 */
@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  email = '';
  password = '';
  submitting = false;
  error: string | null = null;

  constructor(private account: AccountService, private router: Router, private route: ActivatedRoute) {}

  /** Local email/password login. */
  login(): void {
    this.submitting = true;
    this.error = null;
    this.account.login({ email: this.email.trim(), password: this.password }).subscribe({
      next: () => this.redirect(),
      error: () => {
        this.submitting = false;
        this.error = 'Invalid email or password.';
      }
    });
  }

  /**
   * Social login. In this demo the provider credential is simulated as
   * "externalId:email:name" (the real integration verifies a provider token
   * server-side). The buttons exercise the full find-or-create account flow.
   */
  social(provider: 'Google' | 'Facebook'): void {
    this.submitting = true;
    this.error = null;
    // Demo credential: a stable external id + an email derived from the provider.
    const demoCredential = `${provider.toLowerCase()}-uid-1001:${provider.toLowerCase()}.user@demo.local:${provider} User`;
    this.account.socialLogin({ provider, credential: demoCredential }).subscribe({
      next: () => this.redirect(),
      error: () => {
        this.submitting = false;
        this.error = `${provider} sign-in failed.`;
      }
    });
  }

  private redirect(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/account';
    this.router.navigateByUrl(returnUrl);
  }
}
