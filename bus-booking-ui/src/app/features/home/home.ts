import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CityService } from '../../core/services/city.service';
import type { City } from '../../shared/models/city.model';

@Component({
  selector: 'app-home',
  imports: [FormsModule],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class HomeComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly cityService = inject(CityService);

  readonly cities = signal<City[]>([]);
  fromCityId = '';
  toCityId = '';
  travelDate = '';
  error = '';

  get today(): string {
    return new Date().toISOString().split('T')[0];
  }

  async ngOnInit(): Promise<void> {
    try {
      this.cities.set(await this.cityService.getCities());
    } catch { /* cities load is best-effort */ }
  }

  async search(): Promise<void> {
    if (!this.fromCityId || !this.toCityId || !this.travelDate) {
      this.error = 'Please fill in all fields.';
      return;
    }
    if (this.fromCityId === this.toCityId) {
      this.error = 'Origin and destination must be different.';
      return;
    }
    this.error = '';
    await this.router.navigate(['/search'], {
      queryParams: {
        fromCityId: this.fromCityId,
        toCityId: this.toCityId,
        travelDate: this.travelDate,
      },
    });
  }
}
