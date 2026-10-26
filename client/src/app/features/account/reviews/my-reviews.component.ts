import { LocalDatePipe } from '../../../core/i18n/format.pipe';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AccountService } from '../../../core/services/account.service';
import { Review } from '../../../core/models/account.models';

/**
 * Lists the reviews written by the signed-in customer.
 */
@Component({
  selector: 'app-my-reviews',
  standalone: true,
  imports: [LocalDatePipe,TranslatePipe,CommonModule, RouterLink],
  templateUrl: './my-reviews.component.html',
  styleUrl: './my-reviews.component.css'
})
export class MyReviewsComponent implements OnInit {
  reviews: Review[] = [];
  loading = false;
  error: string | null = null;

  constructor(private account: AccountService) {}

  ngOnInit(): void {
    this.loading = true;
    this.account.getMyReviews().subscribe({
      next: list => {
        this.reviews = list;
        this.loading = false;
      },
      error: () => {
        this.error = 'Could not load your reviews.';
        this.loading = false;
      }
    });
  }

  /** Renders a 1-5 rating as filled/empty stars. */
  stars(rating: number): string {
    return '★★★★★'.slice(0, rating) + '☆☆☆☆☆'.slice(0, 5 - rating);
  }
}
