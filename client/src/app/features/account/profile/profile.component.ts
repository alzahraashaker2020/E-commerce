import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AccountService } from '../../../core/services/account.service';
import { CustomerProfile } from '../../../core/models/account.models';

/**
 * Customer profile view/edit. Loads the signed-in customer's profile and lets
 * them update name, phone, and marketing consent (email is read-only).
 */
@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})
export class ProfileComponent implements OnInit {
  profile: CustomerProfile | null = null;
  loading = false;
  saving = false;
  saved = false;
  error: string | null = null;

  constructor(private account: AccountService) {}

  ngOnInit(): void {
    this.loading = true;
    this.account.getProfile().subscribe({
      next: p => {
        this.profile = p;
        this.loading = false;
      },
      error: () => {
        this.error = 'Could not load your profile.';
        this.loading = false;
      }
    });
  }

  /** Persists the editable profile fields. */
  save(): void {
    if (!this.profile) {
      return;
    }
    this.saving = true;
    this.saved = false;
    this.error = null;
    this.account.updateProfile(this.profile).subscribe({
      next: p => {
        this.profile = p;
        this.saving = false;
        this.saved = true;
        setTimeout(() => (this.saved = false), 2500);
      },
      error: () => {
        this.saving = false;
        this.error = 'Could not save your profile.';
      }
    });
  }
}
