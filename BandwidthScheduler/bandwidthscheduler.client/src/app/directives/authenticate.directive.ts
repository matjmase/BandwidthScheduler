import { Directive, Input, TemplateRef, ViewContainerRef } from '@angular/core';
import { Subscription } from 'rxjs';
import { BackendConnectService } from '../services/backend-connect.service';

@Directive({
  selector: '[appAuthenticate]',
})
export class AuthenticateDirective {
  @Input() appAuthenticate = true;

  sub: Subscription | undefined;

  constructor(
    private backend: BackendConnectService,
    private templateRef: TemplateRef<unknown>,
    private vcr: ViewContainerRef
  ) {}
  ngOnInit(): void {
    this.SetView();
    this.sub = this.backend.Login.RolesHaveChanged.subscribe({
      next: () => this.SetView(),
    });
  }
  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  private SetView() {
    if (
      (this.backend.Login.GetSavedResponse() !== undefined &&
        this.appAuthenticate) ||
      (this.backend.Login.GetSavedResponse() === undefined &&
        !this.appAuthenticate)
    ) {
      this.vcr.createEmbeddedView(this.templateRef);
    } else {
      this.vcr.clear();
    }
  }
}
