import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TrackingService } from '../../core/services/tracking.service';
import { OrderTracking } from '../../core/models/api.models';
import { StoreCurrencyPipe } from '../../core/pipes/store-currency.pipe';

/**
 * Order tracking page. Looks up an order by its order number and shows the
 * status, placement date, line items, and total. Prefills the number when the
 * confirmation page links here with a query param.
 */
@Component({
  selector: 'app-tracking',
  standalone: true,
  imports: [CommonModule, FormsModule, StoreCurrencyPipe],
  templateUrl: './tracking.component.html',
  styleUrl: './tracking.component.css'
})
export class TrackingComponent implements OnInit {
  orderNumber = '';
  email = '';
  /** Which lookup the form is currently using. */
  mode: 'number' | 'email' = 'number';
  order: OrderTracking | null = null;
  searched = false;
  loading = false;
  error: string | null = null;

  constructor(private tracking: TrackingService, private route: ActivatedRoute) {}

  ngOnInit(): void {
    const prefill = this.route.snapshot.queryParamMap.get('orderNumber');
    if (prefill) {
      this.orderNumber = prefill;
      this.lookup();
    }
  }

  /** Looks up the order by number. */
  lookup(): void {
    const num = this.orderNumber.trim();
    if (!num) {
      return;
    }
    this.loading = true;
    this.error = null;
    this.searched = true;
    this.tracking.track(num).subscribe({
      next: o => {
        this.order = o;
        this.loading = false;
      },
      error: () => {
        this.order = null;
        this.error = 'No order found with that number.';
        this.loading = false;
      }
    });
  }

  /** Looks up the most recent order placed with the entered email. */
  lookupByEmail(): void {
    const mail = this.email.trim();
    if (!mail) {
      return;
    }
    this.loading = true;
    this.error = null;
    this.searched = true;
    this.tracking.trackByEmail(mail).subscribe({
      next: o => {
        this.order = o;
        this.loading = false;
      },
      error: () => {
        this.order = null;
        this.error = 'No order found for that email.';
        this.loading = false;
      }
    });
  }

  /** Switches the lookup mode and clears any previous result/error. */
  setMode(mode: 'number' | 'email'): void {
    this.mode = mode;
    this.order = null;
    this.error = null;
    this.searched = false;
  }

  /** Runs the lookup matching the current mode. */
  submit(): void {
    if (this.mode === 'email') {
      this.lookupByEmail();
    } else {
      this.lookup();
    }
  }

  /** Maps a status to a badge style. */
  statusClass(status: string): string {
    switch (status) {
      case 'Delivered':
      case 'Paid':
        return 'badge-ok';
      case 'Cancelled':
        return 'badge-warn';
      default:
        return 'badge-muted';
    }
  }
}
