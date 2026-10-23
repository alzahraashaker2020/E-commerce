import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../../core/services/admin.service';

/**
 * Admin customers list: shows registered customers and their marketing-consent
 * state (read-only in this scaffold).
 */
@Component({
  selector: 'app-admin-customers',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-customers.component.html',
  styleUrl: './admin-customers.component.css'
})
export class AdminCustomersComponent implements OnInit {
  customers: any[] = [];
  loading = false;
  error: string | null = null;

  constructor(private admin: AdminService) {}

  ngOnInit(): void {
    this.loading = true;
    this.admin.getCustomers().subscribe({
      next: list => {
        this.customers = list;
        this.loading = false;
      },
      error: () => {
        this.error = 'Could not load customers.';
        this.loading = false;
      }
    });
  }
}
