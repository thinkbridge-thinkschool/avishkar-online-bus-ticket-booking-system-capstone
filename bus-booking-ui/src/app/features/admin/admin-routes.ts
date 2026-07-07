import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { RouteService } from '../../core/services/route.service';
import { CityService } from '../../core/services/city.service';
import { ConfirmDialogService } from '../../core/services/confirm-dialog.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import type { BusRoute } from '../../shared/models/route.model';
import type { City } from '../../shared/models/city.model';

@Component({
  selector: 'app-admin-routes',
  imports: [RouterLink, ReactiveFormsModule, LoadingSpinnerComponent],
  templateUrl: './admin-routes.html',
})
export class AdminRoutesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly routeService = inject(RouteService);
  private readonly cityService = inject(CityService);
  private readonly confirmDialog = inject(ConfirmDialogService);

  readonly routes = signal<BusRoute[]>([]);
  readonly cities = signal<City[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    fromCityId: ['', Validators.required],
    toCityId: ['', Validators.required],
  });

  async ngOnInit(): Promise<void> {
    try {
      const [r, c] = await Promise.all([
        this.routeService.getRoutes(),
        this.cityService.getCities(),
      ]);
      this.routes.set(r);
      this.cities.set(c);
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  cityName(id: string): string {
    return this.cities().find(c => c.cityId === id)?.cityName ?? id;
  }

  async addRoute(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.saving.set(true);
    this.error.set(null);
    try {
      const { fromCityId, toCityId } = this.form.getRawValue();
      await this.routeService.createRoute({
        source: this.cityName(fromCityId),
        destination: this.cityName(toCityId),
      });
      this.routes.set(await this.routeService.getRoutes());
      this.form.reset({ fromCityId: '', toCityId: '' });
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.saving.set(false);
    }
  }

  async deleteRoute(routeId: string): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'Confirm Delete',
      message: 'Are you sure you want to delete this route?',
      confirmText: 'Delete',
      danger: true,
    });
    if (!confirmed) return;
    try {
      await this.routeService.deleteRoute(routeId);
      this.routes.update(list => list.filter(r => r.routeId !== routeId));
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    }
  }
}
