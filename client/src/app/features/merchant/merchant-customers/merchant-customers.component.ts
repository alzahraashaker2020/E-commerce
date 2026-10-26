import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MerchantService } from '../../../core/services/merchant.service';

/**
 * Merchant customers list: shows registered customers and their marketing-consent
 * state (read-only in this scaffold).
 */
@Component({
  selector: 'app-merchant-customers',
  standalone: true,
  imports: [TranslatePipe,CommonModule],
  templateUrl: './merchant-customers.component.html',
  styleUrl: './merchant-customers.component.css'
})
export class MerchantCustomersComponent implements OnInit {
  customers: any[] = [];
  loading = false;
  error: string | null = null;

  constructor(private merchant: MerchantService) {}

  ngOnInit(): void {
    this.loading = true;
    this.merchant.getCustomers().subscribe({
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
