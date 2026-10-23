import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../../core/services/admin.service';
import { StoreCurrencyPipe } from '../../../core/pipes/store-currency.pipe';

/**
 * Admin sales overview dashboard: revenue, order count, average order value,
 * current low-stock count, and the top products by units sold.
 */
@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, StoreCurrencyPipe],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.css'
})
export class AdminDashboardComponent implements OnInit {
  overview: any = null;
  loading = false;
  error: string | null = null;

  constructor(private admin: AdminService) {}

  ngOnInit(): void {
    this.loading = true;
    this.admin.getSalesOverview().subscribe({
      next: data => {
        this.overview = data;
        this.loading = false;
      },
      error: () => {
        this.error = 'Could not load sales overview.';
        this.loading = false;
      }
    });
  }
}
