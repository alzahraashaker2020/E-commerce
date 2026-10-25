import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { StoreCurrencyPipe } from '../../../core/pipes/store-currency.pipe';
import { environment } from '../../../../environments/environment';

/**
 * Admin products management: list all products, create/edit via a variant-aware
 * form (product flags, product-level stock OR specific size/color variant rows,
 * category + dependent subcategory dropdowns, and image uploads with preview),
 * delete, and apply a bulk price change to selected products.
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
  categories: any[] = [];
  sizes: any[] = [];
  colors: any[] = [];
  loading = false;
  error: string | null = null;

  // Inline create/edit form model.
  editing: any = null;
  form: any = this.blankForm();

  // The chosen top-level category id, driving the dependent subcategory list.
  parentCategoryId: number | null = null;

  // Bulk update state.
  selectedIds: Record<number, boolean> = {};
  bulkPrice: number | null = null;

  constructor(private admin: AdminService) {}

  ngOnInit(): void {
    // Load supporting data first so the dropdowns/variant pickers are ready.
    this.admin.getSizes().subscribe({ next: list => (this.sizes = list) });
    this.admin.getColors().subscribe({ next: list => (this.colors = list) });
    this.admin.getAdminCategories().subscribe({ next: list => (this.categories = list) });
    this.refresh();
  }

  blankForm(): any {
    return {
      name: '',
      brand: '',
      basePrice: 0,
      categoryId: null,
      descriptionHtml: '',
      isActive: true,
      slug: '',
      hasVariants: false,
      showStockQuantity: false,
      stockQuantity: 0,
      lowStockThreshold: 0,
      imageUrl: '',
      variants: []
    };
  }

  /** Top-level categories (for the parent dropdown). */
  get topLevelCategories(): any[] {
    return this.categories.filter(c => c.parentCategoryId == null);
  }

  /** Subcategories of the currently chosen parent (for the dependent dropdown). */
  get subCategories(): any[] {
    if (this.parentCategoryId == null) {
      return [];
    }
    return this.categories.filter(c => c.parentCategoryId === this.parentCategoryId);
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
    this.parentCategoryId = null;
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
      hasVariants: p.hasVariants,
      showStockQuantity: p.showStockQuantity,
      stockQuantity: p.stockQuantity ?? 0,
      lowStockThreshold: p.lowStockThreshold ?? 0,
      imageUrl: (p.images && p.images.length) ? p.images[0].url : '',
      variants: (p.variants || []).map((v: any) => ({
        sizeId: v.sizeId ?? null,
        colorId: v.colorId ?? null,
        sku: v.sku ?? '',
        price: v.price,
        stockQuantity: v.stockQuantity,
        lowStockThreshold: v.lowStockThreshold ?? 0,
        isActive: v.isActive,
        imageUrl: v.imageUrl ?? ''
      }))
    };
    // Resolve the parent dropdown so the dependent subcategory list loads: if
    // the product's category is itself a subcategory, select its parent;
    // otherwise the category is top-level and is its own "parent" selection.
    const cat = this.categories.find(c => c.id === p.categoryId);
    this.parentCategoryId = cat?.parentCategoryId ?? cat?.id ?? null;
  }

  /** Adds an empty variant row to the form. */
  addVariant(): void {
    this.form.variants.push({
      sizeId: null,
      colorId: null,
      sku: '',
      price: this.form.basePrice ?? 0,
      stockQuantity: 0,
      lowStockThreshold: 0,
      isActive: true,
      imageUrl: ''
    });
  }

  /** Removes the variant row at the given index. */
  removeVariant(index: number): void {
    this.form.variants.splice(index, 1);
  }

  /** Builds an absolute URL for previewing a stored relative upload path. */
  previewUrl(path: string | null | undefined): string {
    if (!path) {
      return '';
    }
    if (/^https?:\/\//i.test(path) || path.startsWith('assets/')) {
      return path;
    }
    // Relative upload path like /uploads/x.png — serve it from the API origin.
    const origin = environment.apiBaseUrl.replace(/\/api\/?$/, '');
    return path.startsWith('/') ? origin + path : origin + '/' + path;
  }

  /** Uploads the selected file and stores the returned path on the product. */
  onProductImage(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files && input.files[0];
    if (!file) {
      return;
    }
    this.error = null;
    this.admin.uploadImage(file).subscribe({
      next: res => {
        if (res.success) {
          this.form.imageUrl = res.data;
        } else {
          this.error = res.message ?? 'Upload failed.';
        }
      },
      error: err => (this.error = err?.error?.message ?? 'Upload failed.')
    });
  }

  /** Uploads the selected file and stores the returned path on a variant row. */
  onVariantImage(event: Event, variant: any): void {
    const input = event.target as HTMLInputElement;
    const file = input.files && input.files[0];
    if (!file) {
      return;
    }
    this.error = null;
    this.admin.uploadImage(file).subscribe({
      next: res => {
        if (res.success) {
          variant.imageUrl = res.data;
        } else {
          this.error = res.message ?? 'Upload failed.';
        }
      },
      error: err => (this.error = err?.error?.message ?? 'Upload failed.')
    });
  }

  /** Clears the product-level image. */
  removeProductImage(): void {
    this.form.imageUrl = '';
  }

  /** Clears a variant image. */
  removeVariantImage(variant: any): void {
    variant.imageUrl = '';
  }

  save(): void {
    this.error = null;
    const body = this.buildPayload();
    const op = this.editing
      ? this.admin.updateProduct(this.editing.id, body)
      : this.admin.createProduct(body);
    op.subscribe({
      next: () => {
        this.startCreate();
        this.refresh();
      },
      error: err => (this.error = err?.error?.message ?? 'Save failed.')
    });
  }

  /**
   * Assembles the ProductUpsertRequest payload: product flags/stock plus, when
   * HasVariants is true, the chosen variant rows only (no auto matrix).
   */
  private buildPayload(): any {
    return {
      name: this.form.name,
      slug: this.form.slug,
      brand: this.form.brand,
      basePrice: this.form.basePrice,
      categoryId: this.form.categoryId,
      descriptionHtml: this.form.descriptionHtml,
      isActive: this.form.isActive,
      hasVariants: this.form.hasVariants,
      showStockQuantity: this.form.showStockQuantity,
      stockQuantity: this.form.hasVariants ? 0 : this.form.stockQuantity,
      lowStockThreshold: this.form.hasVariants ? 0 : this.form.lowStockThreshold,
      variants: this.form.hasVariants
        ? this.form.variants.map((v: any) => ({
            sizeId: v.sizeId || null,
            colorId: v.colorId || null,
            sku: v.sku || null,
            price: v.price,
            stockQuantity: v.stockQuantity,
            lowStockThreshold: v.lowStockThreshold,
            isActive: v.isActive,
            imageUrl: v.imageUrl || null
          }))
        : []
    };
  }

  remove(p: any): void {
    this.admin.deleteProduct(p.id).subscribe({
      next: () => this.refresh(),
      error: () => (this.error = 'Delete failed.')
    });
  }

  /** Builds a readable stock label for the product table. */
  stockLabel(p: any): string {
    if (p.hasVariants) {
      const total = (p.variants || []).reduce((sum: number, v: any) => sum + (v.stockQuantity || 0), 0);
      return `${total} (variants)`;
    }
    return String(p.stockQuantity ?? 0);
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
