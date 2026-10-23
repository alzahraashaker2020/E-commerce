import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

/**
 * Admin login page. Authenticates against api/auth/login and, on success,
 * navigates to the intended admin destination (returnUrl) or the dashboard.
 */
@Component({
  selector: 'app-admin-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-login.component.html',
  styleUrl: './admin-login.component.css'
})
export class AdminLoginComponent {
  username = '';
  password = '';
  submitting = false;
  error: string | null = null;

  constructor(private auth: AuthService, private router: Router, private route: ActivatedRoute) {}

  /** Submits the credentials and redirects on success. */
  login(): void {
    this.submitting = true;
    this.error = null;
    this.auth.login({ username: this.username.trim(), password: this.password }).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/admin/dashboard';
        this.router.navigateByUrl(returnUrl);
      },
      error: () => {
        this.submitting = false;
        this.error = 'Invalid credentials.';
      }
    });
  }
}
