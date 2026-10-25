import { Component, OnInit } from '@angular/core';
import { firstValueFrom } from 'rxjs';
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
  uploading = false;
  saving = false;
  savedNotice: string | null = null;
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
      imageUrls: [],
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
    if (this.uploading) return;
    this.editing = null;
    this.parentCategoryId = null;
    this.form = this.blankForm();
  }

  startEdit(p: any): void {
    if (this.uploading) return;
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
      imageUrls: (p.images || []).slice().sort((a: any, b: any) => a.sortOrder - b.sortOrder).map((img: any) => img.url),
      variants: (p.variants || []).map((v: any) => ({
        id: v.id,
        sizeId: v.sizeId ?? null,
        colorId: v.colorId ?? null,
        sku: v.sku ?? '',
        price: v.price,
        stockQuantity: v.stockQuantity,
        lowStockThreshold: v.lowStockThreshold ?? 0,
        isActive: v.isActive,
        imageUrl: v.imageUrl ?? '',
        imageUrls: v.imageUrls?.length ? [...v.imageUrls] : (v.imageUrl ? [v.imageUrl] : [])
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
      imageUrl: '',
      imageUrls: []
    });
  }

  /** Removes the variant row at the given index. */
  removeVariant(index: number): void {
    if (this.uploading) return;
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

  onProductImage(event: Event): void { void this.uploadImages(event, this.form); }
  onVariantImage(event: Event, variant: any): void { void this.uploadImages(event, variant); }

  private async uploadImages(event: Event, target: any): Promise<void> {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files || []);
    if (!files.length || this.uploading) return;
    if (target.imageUrls.length + files.length > 20) {
      this.error = 'Maximum 20 images per product or variant.';
      input.value = '';
      return;
    }
    this.uploading = true;
    this.error = null;
    try {
      for (const file of files) {
        const res = await firstValueFrom(this.admin.uploadImage(file));
        if (!res.success || !res.data) throw new Error(res.message || 'Upload failed.');
        target.imageUrls.push(res.data);
      }
    } catch (err: any) {
      this.error = err?.error?.message || err?.message || 'Upload failed. Already uploaded images are retained.';
    } finally {
      target.imageUrl = target.imageUrls[0] || '';
      this.uploading = false;
      input.value = '';
    }
  }

  removeImage(target: any, index: number): void {
    target.imageUrls.splice(index, 1);
    target.imageUrl = target.imageUrls[0] || '';
  }

  makePrimary(target: any, index: number): void {
    const [image] = target.imageUrls.splice(index, 1);
    target.imageUrls.unshift(image);
    target.imageUrl = image;
  }
  save(): void {
    if (this.uploading || this.saving) return;
    this.error = null;
    this.savedNotice = null;
    this.saving = true;
    const body = this.buildPayload();
    const op = this.editing
      ? this.admin.updateProduct(this.editing.id, body)
      : this.admin.createProduct(body);
    op.subscribe({
      next: saved => {
        this.saving = false;
        if (!saved?.id) {
          this.error = 'The server did not confirm saving the product. Your form has been kept.';
          return;
        }
        // Keep the saved ID on failure so retrying cannot create another product.
        this.editing = { id: saved.id };
        const actualImages = (saved.images || []).slice().sort((a: any, b: any) => a.sortOrder - b.sortOrder).map((i: any) => i.url);
        const matches = (a: string[], b: string[]) => JSON.stringify(a) === JSON.stringify(b);
        let imagesSaved = matches(actualImages, body.imageUrls);
        for (const variant of this.form.variants) {
          const actual = (saved.variants || []).find((v: any) => variant.id ? v.id === variant.id : v.sizeId === variant.sizeId && v.colorId === variant.colorId);
          if (actual) variant.id = actual.id;
          if (body.hasVariants && (!actual || !matches(actual.imageUrls || [], variant.imageUrls))) imagesSaved = false;
        }
        if (!imagesSaved) {
          this.error = 'The server did not save all selected images. Your selections have been kept. Restart the backend with the latest version, then click Update.';
          return;
        }
        this.startCreate();
        this.savedNotice = 'Product and images saved successfully.';
        this.refresh();
      },
      error: err => {
        this.saving = false;
        this.error = err?.error?.message ?? 'Save failed. Your images and form have been kept.';
      }
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
      imageUrl: this.form.imageUrl || '',
      imageUrls: [...this.form.imageUrls],
      isActive: this.form.isActive,
      hasVariants: this.form.hasVariants,
      showStockQuantity: this.form.showStockQuantity,
      stockQuantity: this.form.hasVariants ? 0 : this.form.stockQuantity,
      lowStockThreshold: this.form.hasVariants ? 0 : this.form.lowStockThreshold,
      variants: this.form.hasVariants
        ? this.form.variants.map((v: any) => ({
            id: v.id || null,
            sizeId: v.sizeId || null,
            colorId: v.colorId || null,
            sku: v.sku || null,
            price: v.price,
            stockQuantity: v.stockQuantity,
            lowStockThreshold: v.lowStockThreshold,
            isActive: v.isActive,
            imageUrl: v.imageUrl || null,
            imageUrls: [...v.imageUrls]
          }))
        : []
    };
  }

  remove(p: any): void {
    if (this.uploading) return;
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
