import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';

/**
 * Admin color management: list colors (with a hex swatch), create/edit via an
 * inline form, and delete. Deleting a color still referenced by a product
 * variant surfaces the server 409 message.
 */
@Component({
  selector: 'app-admin-colors',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-colors.component.html',
  styleUrl: './admin-colors.component.css'
})
export class AdminColorsComponent implements OnInit {
  colors: any[] = [];
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
    return { name: '', hexCode: '#000000', displayOrder: 0, isActive: true };
  }

  refresh(): void {
    this.loading = true;
    this.admin.getColors().subscribe({
      next: list => {
        this.colors = list;
        this.loading = false;
      },
      error: () => {
        this.error = 'Could not load colors.';
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
      hexCode: c.hexCode ?? '#000000',
      displayOrder: c.displayOrder,
      isActive: c.isActive
    };
  }

  cancel(): void {
    this.editing = null;
    this.form = this.blankForm();
  }

  save(): void {
    this.error = null;

    if (this.editing) {
      this.admin.updateColor(this.editing.id, this.form).subscribe({
        next: () => {
          this.cancel();
          this.refresh();
        },
        error: err => (this.error = err?.error?.message ?? 'Save failed.')
      });
      return;
    }

    this.admin.createColor(this.form).subscribe({
      next: res => {
        if (res.success) {
          this.cancel();
          this.refresh();
        } else {
          this.error = res.message ?? 'Create failed.';
        }
      },
      error: err => (this.error = err?.error?.message ?? 'Create failed.')
    });
  }

  /** Deletes a color; surfaces the 409 message when it is still referenced. */
  remove(c: any): void {
    this.error = null;
    this.admin.deleteColor(c.id).subscribe({
      next: () => this.refresh(),
      error: err => (this.error = err?.error?.message ?? 'Delete failed.')
    });
  }
}
