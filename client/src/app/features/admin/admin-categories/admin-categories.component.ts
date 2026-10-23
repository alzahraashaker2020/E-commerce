import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';

/**
 * Admin category management: list top-level categories and subcategories (with
 * parent names and child/product counts), create/edit via an inline form with a
 * parent picker, and delete. Deleting a category that still has subcategories or
 * products surfaces the server 409 message.
 */
@Component({
  selector: 'app-admin-categories',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-categories.component.html',
  styleUrl: './admin-categories.component.css'
})
export class AdminCategoriesComponent implements OnInit {
  categories: any[] = [];
  loading = false;
  error: string | null = null;

  // Inline create/edit form model.
  editing: any = null;
  form: any = this.blankForm();

  constructor(private admin: AdminService) {}

  ngOnInit(): void {
    this.refresh();
  }

  blankForm(): any {
    return { name: '', description: '', imageUrl: '', parentCategoryId: null };
  }

  /** Top-level categories only — used to populate the parent picker. */
  get topLevel(): any[] {
    return this.categories.filter(c => c.parentCategoryId == null);
  }

  refresh(): void {
    this.loading = true;
    this.admin.getAdminCategories().subscribe({
      next: list => {
        this.categories = list;
        this.loading = false;
      },
      error: () => {
        this.error = 'Could not load categories.';
        this.loading = false;
      }
    });
  }

  startCreate(): void {
    this.editing = null;
    this.form = this.blankForm();
  }

  startEdit(c: any): void {
    this.editing = c;
    this.form = {
      name: c.name,
      description: c.description,
      imageUrl: c.imageUrl,
      parentCategoryId: c.parentCategoryId ?? null
    };
  }

  cancel(): void {
    this.editing = null;
    this.form = this.blankForm();
  }

  save(): void {
    this.error = null;
    // Normalise the parent picker's blank selection to a real null.
    const body = { ...this.form, parentCategoryId: this.form.parentCategoryId || null };

    if (this.editing) {
      this.admin.updateCategory(this.editing.id, body).subscribe({
        next: () => {
          this.cancel();
          this.refresh();
        },
        error: () => (this.error = 'Save failed.')
      });
      return;
    }

    this.admin.createCategory(body).subscribe({
      next: res => {
        if (res.success) {
          this.cancel();
          this.refresh();
        } else {
          this.error = res.message ?? 'Create failed.';
        }
      },
      error: err => {
        this.error = err?.error?.message ?? 'Create failed.';
      }
    });
  }

  /** Deletes a category; surfaces the 409 message when it has children/products. */
  remove(c: any): void {
    this.error = null;
    this.admin.deleteCategory(c.id).subscribe({
      next: () => this.refresh(),
      error: err => {
        this.error = err?.error?.message ?? 'Delete failed.';
      }
    });
  }
}
