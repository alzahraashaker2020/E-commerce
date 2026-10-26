import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AccountService } from '../../../core/services/account.service';

/**
 * Customer registration page: creates a local account and signs the customer
 * in on success (the service stores the returned JWT).
 */
@Component({
  selector: 'app-register',
  standalone: true,
  imports: [TranslatePipe,CommonModule, FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  fullName = '';
  email = '';
  password = '';
  phone = '';
  marketingConsent = false;
  submitting = false;
  error: string | null = null;

  constructor(private account: AccountService, private router: Router) {}

  get canSubmit(): boolean {
    return (
      this.fullName.trim().length > 0 &&
      this.email.trim().length > 3 &&
      this.password.length >= 6 &&
      !this.submitting
    );
  }

  /** Submits the registration and redirects to the account area on success. */
  register(): void {
    if (!this.canSubmit) {
      return;
    }
    this.submitting = true;
    this.error = null;
    this.account
      .register({
        fullName: this.fullName.trim(),
        email: this.email.trim(),
        password: this.password,
        phone: this.phone.trim() || null,
        marketingConsent: this.marketingConsent
      })
      .subscribe({
        next: () => this.router.navigate(['/account']),
        error: err => {
          this.submitting = false;
          this.error = err?.error?.message ?? 'Registration failed (email may already be in use).';
        }
      });
  }
}
