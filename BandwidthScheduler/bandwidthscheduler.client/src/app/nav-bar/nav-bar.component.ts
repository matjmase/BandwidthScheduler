import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { BackendConnectService } from '../services/backend-connect.service';

@Component({
  selector: 'app-nav-bar',
  templateUrl: './nav-bar.component.html',
  styleUrl: './nav-bar.component.scss',
})
export class NavBarComponent {
  isExpanded: boolean = false;

  constructor(private router: Router, private backend: BackendConnectService) {}

  public ToggleExpand(): void {
    this.isExpanded = !this.isExpanded;
  }

  public SubMenuNavigationClick(path: string): void {
    this.isExpanded = false;

    this.router.navigateByUrl(path);
  }

  public LogoutClicked(): void {
    this.isExpanded = false;

    this.backend.Login.Logout();
    this.router.navigate(['']);
  }
}
