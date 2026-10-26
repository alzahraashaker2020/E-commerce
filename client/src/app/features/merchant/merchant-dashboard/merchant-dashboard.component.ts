import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MerchantService } from '../../../core/services/merchant.service';
import { StoreCurrencyPipe } from '../../../core/pipes/store-currency.pipe';
import { RouterLink } from '@angular/router';

/**
 * Merchant sales overview dashboard: revenue, order count, average order value,
 * current low-stock count, and the top products by units sold.
 */
@Component({
  selector: 'app-merchant-dashboard',
  standalone: true,
  imports: [TranslatePipe,CommonModule, StoreCurrencyPipe, RouterLink],
  templateUrl: './merchant-dashboard.component.html',
  styleUrl: './merchant-dashboard.component.css'
})
export class MerchantDashboardComponent implements OnInit {
  overview: any = null;
  loading = false;
  error: string | null = null;

  constructor(private merchant: MerchantService) {}

  ngOnInit(): void {
    this.loading = true;
    this.merchant.getSalesOverview().subscribe({
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
