import { Component, OnInit, TemplateRef } from '@angular/core';
import { NgForm } from '@angular/forms';
import { LoginTemplateModel } from '../login/login.component';
import { BackendConnectService } from '../services/backend-connect.service';
import { RegisterCredentials } from '../models/IRegisterCredentials';
import { Router } from '@angular/router';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent implements OnInit {
  roles: string[] = [];

  constructor(private backend: BackendConnectService, private router: Router) {}

  ngOnInit(): void {
    this.backend.Login.GetAllRoles().subscribe({
      next: (val) => (this.roles = val),
    });
  }

  public Submit(form: NgForm): void {
    const credentials = new RegisterCredentials(form.value);

    this.backend.Login.Register(credentials).subscribe({
      complete: () => this.router.navigate(['/home']),
    });
  }
}

export interface RegisterTemplateModel<T> extends LoginTemplateModel<T> {
  verify: T;
  type: T;
  roles: Object;
}
