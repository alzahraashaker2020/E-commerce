import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MerchantService } from '../../../core/services/merchant.service';

/**
 * Merchant inventory alerts: lists every inventory row currently at or below its
 * low-stock threshold so the owner can reorder.
 */
@Component({
  selector: 'app-merchant-inventory',
  standalone: true,
  imports: [TranslatePipe,CommonModule],
  templateUrl: './merchant-inventory.component.html',
  styleUrl: './merchant-inventory.component.css'
})
export class MerchantInventoryComponent implements OnInit {
  items: any[] = [];
  loading = false;
  error: string | null = null;

  constructor(private merchant: MerchantService) {}

  ngOnInit(): void {
    this.loading = true;
    this.merchant.getLowStock().subscribe({
      next: list => {
        this.items = list;
        this.loading = false;
      },
      error: () => {
        this.error = 'Could not load inventory alerts.';
        this.loading = false;
      }
    });
  }
}
