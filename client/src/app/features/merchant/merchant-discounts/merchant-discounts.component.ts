import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MerchantService } from '../../../core/services/merchant.service';
import { StoreCurrencyPipe } from '../../../core/pipes/store-currency.pipe';

/**
 * Merchant discounts management: list discounts, create a new one (duplicate codes
 * are rejected by the server with 409), and delete.
 */
@Component({
  selector: 'app-merchant-discounts',
  standalone: true,
  imports: [TranslatePipe,CommonModule, FormsModule, StoreCurrencyPipe],
  templateUrl: './merchant-discounts.component.html',
  styleUrl: './merchant-discounts.component.css'
})
export class MerchantDiscountsComponent implements OnInit {
  discounts: any[] = [];
  loading = false;
  error: string | null = null;

  // Create form model.
  form = this.blankForm();

  constructor(private merchant: MerchantService) {}

  ngOnInit(): void {
    this.refresh();
  }

  blankForm(): any {
    return { code: '', type: 'Percentage', value: 10, isActive: true, startsAt: null, endsAt: null };
  }

  refresh(): void {
    this.loading = true;
    this.merchant.getDiscounts().subscribe({
      next: list => {
        this.discounts = list;
        this.loading = false;
      },
      error: () => {
        this.error = 'Could not load discounts.';
        this.loading = false;
      }
    });
  }

  /** Creates a discount; surfaces the 409 duplicate-code message when relevant. */
  create(): void {
    this.error = null;
    this.merchant.createDiscount(this.form).subscribe({
      next: res => {
        if (res.success) {
          this.form = this.blankForm();
          this.refresh();
        } else {
          this.error = res.message ?? 'Create failed.';
        }
      },
      error: err => {
        this.error = err?.error?.message ?? 'Create failed (code may already exist).';
      }
    });
  }

  remove(d: any): void {
    this.merchant.deleteDiscount(d.id).subscribe({
      next: () => this.refresh(),
      error: () => (this.error = 'Delete failed.')
    });
  }
}
