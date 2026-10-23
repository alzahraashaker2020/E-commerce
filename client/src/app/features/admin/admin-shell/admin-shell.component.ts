import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

/**
 * Admin shell: a sidebar of management links plus the routed admin outlet.
 * Rendered only for authenticated admins (guarded by adminGuard).
 */
@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './admin-shell.component.html',
  styleUrl: './admin-shell.component.css'
})
export class AdminShellComponent {
  username: string | null;

  constructor(private auth: AuthService, private router: Router) {
    // Assigned in the constructor body so `auth` is initialised first (TS2729).
    this.username = this.auth.getUsername();
  }

  /** Signs out and returns to the admin login. */
  logout(): void {
    this.auth.logout();
    this.router.navigate(['/admin/login']);
  }
}
