import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
import { CatalogService } from '../../core/services/catalog.service';
import { Category } from '../../core/models/api.models';
import { ImageUrlPipe } from '../../core/pipes/image-url.pipe';

/**
 * Subcategory drill-down page. Given a top-level category slug (e.g.
 * "sofa-styling"), shows that category's name as a heading and its
 * subcategories as cards (mirroring the reference "SOFA STYLING" layout).
 * Selecting a subcategory navigates to the catalog filtered by it.
 */
@Component({
  selector: 'app-category',
  standalone: true,
  imports: [TranslatePipe,CommonModule, RouterLink, ImageUrlPipe],
  templateUrl: './category.component.html',
  styleUrl: './category.component.css'
})
export class CategoryComponent implements OnInit {
  category: Category | null = null;
  loading = false;
  error: string | null = null;

  constructor(
    private catalog: CatalogService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    // React to slug changes so navigating between categories reloads cleanly.
    this.loading = true;
    this.route.paramMap
      .pipe(switchMap(params => this.catalog.getCategoryBySlug(params.get('slug') ?? '')))
      .subscribe({
        next: cat => {
          this.category = cat;
          this.loading = false;
        },
        error: () => {
          this.error = 'Category not found.';
          this.loading = false;
        }
      });
  }

  /** Opens the catalog filtered to the chosen subcategory. */
  openSubcategory(sub: Category): void {
    this.router.navigate(['/catalog'], { queryParams: { categoryId: sub.id, page: 1 } });
  }

  /** Opens the catalog filtered to the whole top-level category. */
  shopAll(): void {
    if (this.category) {
      this.router.navigate(['/catalog'], { queryParams: { categoryId: this.category.id, page: 1 } });
    }
  }
}
