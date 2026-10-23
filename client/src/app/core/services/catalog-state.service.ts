import { Injectable } from '@angular/core';

/** Snapshot of the shopper's catalog browse position. */
export interface CatalogBrowseState {
  page: number;
  pageSize: number;
  categoryId: number | null;
  scrollY: number;
}

/**
 * Remembers the shopper's catalog browse position (page, selected category,
 * and scroll offset) so that navigating into a product and pressing "back"
 * returns them exactly where they were — the preserved-position pagination
 * differentiator called out in the strategy docs.
 *
 * The state is held in memory for the session and mirrored to sessionStorage
 * so it also survives a full-page reload of the catalog route.
 */
@Injectable({ providedIn: 'root' })
export class CatalogStateService {
  private static readonly KEY = 'shop_catalog_state';
  private state: CatalogBrowseState | null = this.load();

  /** Stores the current browse position. */
  save(state: CatalogBrowseState): void {
    this.state = state;
    try {
      sessionStorage.setItem(CatalogStateService.KEY, JSON.stringify(state));
    } catch {
      // sessionStorage may be unavailable; in-memory copy still works.
    }
  }

  /** Returns the last stored browse position, or null if none. */
  get(): CatalogBrowseState | null {
    return this.state;
  }

  /** Clears the stored position (e.g. when the shopper picks a new filter). */
  clear(): void {
    this.state = null;
    try {
      sessionStorage.removeItem(CatalogStateService.KEY);
    } catch {
      // ignore
    }
  }

  private load(): CatalogBrowseState | null {
    try {
      const raw = sessionStorage.getItem(CatalogStateService.KEY);
      return raw ? (JSON.parse(raw) as CatalogBrowseState) : null;
    } catch {
      return null;
    }
  }
}
