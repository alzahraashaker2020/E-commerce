import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { OrderConfirmation } from '../../core/models/api.models';
import { StoreCurrencyPipe } from '../../core/pipes/store-currency.pipe';

/**
 * Order confirmation page. Shows the generated order number (also usable as the
 * tracking number), any server message (e.g. a discount not applied notice),
 * and — when the checkout page passed the confirmation payload via router state
 * — the shipping address, payment method, and total in the store currency.
 */
@Component({
  selector: 'app-confirmation',
  standalone: true,
  imports: [TranslatePipe,CommonModule, RouterLink, StoreCurrencyPipe],
  templateUrl: './confirmation.component.html',
  styleUrl: './confirmation.component.css'
})
export class ConfirmationComponent implements OnInit {
  orderNumber = '';
  message: string | null = null;
  confirmation: OrderConfirmation | null = null;

  constructor(private route: ActivatedRoute, private router: Router) {}

  ngOnInit(): void {
    this.orderNumber = this.route.snapshot.paramMap.get('orderNumber') ?? '';
    // The checkout page passes any server message and the confirmation payload
    // via router navigation state.
    this.message = (history.state?.message as string) ?? null;
    this.confirmation = (history.state?.confirmation as OrderConfirmation) ?? null;
  }

  /** Jumps to the tracking page prefilled with this order number. */
  track(): void {
    this.router.navigate(['/track'], { queryParams: { orderNumber: this.orderNumber } });
  }
}
