import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MsalService } from '@azure/msal-angular';
import { AuthService } from './core/services/auth.service';
import { NavBarComponent } from './shared/components/nav-bar/nav-bar';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, NavBarComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  constructor(
    private readonly msal: MsalService,
    readonly auth: AuthService
  ) {}

  ngOnInit(): void {
    this.auth.initialize();
  }
}
