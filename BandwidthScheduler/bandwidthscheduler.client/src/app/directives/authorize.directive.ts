import { Directive, Input, TemplateRef, ViewContainerRef } from '@angular/core';
import { Subscription } from 'rxjs';
import { BackendConnectService } from '../services/backend-connect.service';

@Directive({
  selector: '[appAuthorize]',
})
export class AuthorizeDirective {
  authValue: string = '';

  sub: Subscription | undefined;

  @Input() set appAuthorize(val: string) {
    this.authValue = val;
    this.SetView();
  }

  constructor(
    private backend: BackendConnectService,
    private templateRef: TemplateRef<unknown>,
    private vcr: ViewContainerRef
  ) {}

  ngOnInit(): void {
    this.sub = this.backend.Login.RolesHaveChanged.subscribe({
      next: () => this.SetView(),
    });
  }
  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  private SetView() {
    const roles = this.authValue.split(',').map((e) => e.trim());

    let anyMatch = false;

    const savedResp = this.backend.Login.GetSavedResponse();

    if (savedResp) {
      roles.forEach((role) => {
        anyMatch = anyMatch || savedResp.roles.some((e) => e === role);
      });
    }

    if (anyMatch) {
      this.vcr.createEmbeddedView(this.templateRef);
    } else {
      this.vcr.clear();
    }
  }
}
