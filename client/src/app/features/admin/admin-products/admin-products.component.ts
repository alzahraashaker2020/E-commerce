import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { StoreCurrencyPipe } from '../../../core/pipes/store-currency.pipe';

/**
 * Admin products management: list all products, create/edit via a simple form,
 * delete, and apply a bulk price/active change to selected products.
 */
@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [CommonModule, FormsModule, StoreCurrencyPipe],
  templateUrl: './admin-products.component.html',
  styleUrl: './admin-products.component.css'
})
export class AdminProductsComponent implements OnInit {
  products: any[] = [];
  loading = false;
  error: string | null = null;

  // Inline create/edit form model.
  editing: any = null;
  form: any = this.blankForm();

  // Bulk update state.
  selectedIds: Record<number, boolean> = {};
  bulkPrice: number | null = null;

  constructor(private admin: AdminService) {}

  ngOnInit(): void {
    this.refresh();
  }

  blankForm(): any {
    return {
      name: '',
      brand: '',
      basePrice: 0,
      categoryId: 1,
      descriptionHtml: '',
      isActive: true,
      slug: '',
      colour: '',
      size: '',
      width: null,
      height: null,
      quantityOnHand: 0,
      lowStockThreshold: 0
    };
  }

  refresh(): void {
    this.loading = true;
    this.admin.getProducts().subscribe({
      next: list => {
        this.products = list;
        this.loading = false;
      },
      error: () => {
        this.error = 'Could not load products.';
        this.loading = false;
      }
    });
  }

  startCreate(): void {
    this.editing = null;
    this.form = this.blankForm();
  }

  startEdit(p: any): void {
    this.editing = p;
    this.form = {
      name: p.name,
      brand: p.brand,
      basePrice: p.basePrice,
      categoryId: p.categoryId,
      descriptionHtml: p.descriptionHtml,
      isActive: p.isActive,
      slug: p.slug,
      colour: p.colour,
      size: p.size,
      width: p.width,
      height: p.height,
      quantityOnHand: p.quantityOnHand,
      lowStockThreshold: p.lowStockThreshold
    };
  }

  save(): void {
    const op = this.editing
      ? this.admin.updateProduct(this.editing.id, this.form)
      : this.admin.createProduct(this.form);
    op.subscribe({
      next: () => {
        this.editing = null;
        this.form = this.blankForm();
        this.refresh();
      },
      error: () => (this.error = 'Save failed.')
    });
  }

  remove(p: any): void {
    this.admin.deleteProduct(p.id).subscribe({
      next: () => this.refresh(),
      error: () => (this.error = 'Delete failed.')
    });
  }

  applyBulk(): void {
    const ids = Object.keys(this.selectedIds).filter(k => this.selectedIds[+k]).map(k => +k);
    if (!ids.length || this.bulkPrice === null) {
      return;
    }
    this.admin.bulkUpdateProducts({ productIds: ids, newPrice: this.bulkPrice }).subscribe({
      next: () => {
        this.selectedIds = {};
        this.bulkPrice = null;
        this.refresh();
      },
      error: () => (this.error = 'Bulk update failed.')
    });
  }
}
