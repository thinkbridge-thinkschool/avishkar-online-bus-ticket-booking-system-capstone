import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CityService } from '../../core/services/city.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import type { City } from '../../shared/models/city.model';

@Component({
  selector: 'app-admin-cities',
  imports: [RouterLink, ReactiveFormsModule, LoadingSpinnerComponent],
  templateUrl: './admin-cities.html',
})
export class AdminCitiesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly cityService = inject(CityService);

  readonly cities = signal<City[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    cityName: ['', [Validators.required, Validators.minLength(2)]],
  });

  async ngOnInit(): Promise<void> {
    try {
      this.cities.set(await this.cityService.getCities());
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  async addCity(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.saving.set(true);
    this.error.set(null);
    try {
      await this.cityService.createCity(this.form.getRawValue());
      this.cities.set(await this.cityService.getCities());
      this.form.reset();
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.saving.set(false);
    }
  }

  async deleteCity(cityId: string): Promise<void> {
    if (!confirm('Delete this city?')) return;
    try {
      await this.cityService.deleteCity(cityId);
      this.cities.update(list => list.filter(c => c.cityId !== cityId));
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    }
  }
}
