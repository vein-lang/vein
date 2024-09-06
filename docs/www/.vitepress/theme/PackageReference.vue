<template lang="html">
    <div class="package" v-on:click="openPackage">
        <div class="vs-card-content type-3" style="padding: 10px;">
            <div class="vs-card">
                <div class="vs-card__img"><img width="94" height="94"
                        :src="`https://api.vein-lang.org/@/packages/${packageName}/latest/icon`" alt=""
                        style="min-width: 94px;">
                </div>
                <div class="vs-card__text">
                    <div class="vs-card__title">
                        <h3 style="display: inline-flex;"> {{ packageName }} <!---->
                            <div style="padding-left: 5px; padding-right: 5px; color: rgb(70, 70, 70);">•
                            </div>
                            <div class="vs-tooltip-content" style="display: inline;">
                                <i class="bx bx-badge-check" style="color: rgb(99, 99, 221);"></i></div>
                        </h3>
                    </div>
                    <div class="card-text">
                        <p data-v-eac9507a="">
                            A IshtarVM runtime for Vein Lang
                        </p>
                        <div class="card-time" style="min-width: 300px;">
                            <div placeholder="Danger icon after"
                                class="vs-input-parent vs-input-parent--state-null vs-component--danger vs-component--is-color ">
                                <div class="vs-input-content vs-input-content--has-color">
                                    <input disabled="disabled" id="vs-input--2445"
                                        class="vs-input vs-input--has-icon vs-input--has-icon--after"
                                        style="min-width: 100%; opacity: 1;" :value="getValue">
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>
<script>
export default {
    name: "PackageReference",
    props: {
        val: {
            type: String,
            default: '<none>'
        },
        packageName: {
            type: String,
            default: '<none>'
        }
    },
    computed: {
        getValue() {
            return `rune ${this.val}`;
        },
        getUrl() {
            return `https://gallery.vein-lang.org/package/${this.packageName}/latest`;
        }
    },
    methods: {
        openPackage() {
            window.open(this.getUrl, '_blank');
        }
    }
}
</script>
<style scoped>
.package>.vs-card-content.type-3>.vs-card {
    max-width: 100% !important;
}

.vs-card-content.type-3 .vs-card {
    display: -webkit-box;
    display: -ms-flexbox;
    display: flex;
    max-width: 400px;
}

.vs-card {
    background: rgba(var(--vs-background), 1);
    color: rgba(var(--vs-text), 1);
    width: 100%;
    max-width: 350px;
    -webkit-box-shadow: 0 5px 20px 0 rgba(0, 0, 0, var(--vs-shadow-opacity));
    box-shadow: 0 5px 20px 0 rgba(0, 0, 0, var(--vs-shadow-opacity));
    border-radius: 20px;
    -webkit-transition: all .25s ease;
    transition: all .25s ease;
    cursor: pointer;
    position: relative;
}

.vs-card__img,
.vs-card__img img {
    border-radius: 20px;
    -webkit-transition: all .25s ease;
    transition: all .25s ease;
}

.vs-card__img {
    position: relative;
    max-height: 250px;
    overflow: hidden;
    -webkit-box-pack: center;
    -ms-flex-pack: center;
    justify-content: center;
}

.vs-card__img,
.vs-card__interactions {
    display: -webkit-box;
    display: -ms-flexbox;
    display: flex;
    -webkit-box-align: center;
    -ms-flex-align: center;
    align-items: center;
}

.package>.vs-card-content.type-3>.vs-card>.vs-card__img {
    min-width: 94px !important;
}

.package>.vs-card-content.type-3>.vs-card>.vs-card__text {
    width: 100% !important;
}

.vs-card-content.type-3 .vs-card__text {
    padding-top: 15px;
}

.vs-card__text {
    font-size: .85rem;
    padding: 0 15px;
    padding-bottom: 15px;
}

.vs-card__title {
    padding-top: 10px;
    padding-bottom: 5px;
}

.card-text {
    display: flex;
    align-items: flex-end;
    justify-content: space-between;
}

.vs-card-content.type-3 .vs-card__title h2,
.vs-card-content.type-3 .vs-card__title h3,
.vs-card-content.type-3 .vs-card__title h4,
.vs-card-content.type-3 .vs-card__title h5,
.vs-card-content.type-3 .vs-card__title h6 {
    padding: 0;
}

.vs-card__title h2,
.vs-card__title h3,
.vs-card__title h4,
.vs-card__title h5,
.vs-card__title h6 {
    padding: 0;
    margin: 0;
    font-size: 1.1rem;
}

.card-time {
    flex-shrink: 0;
    padding-left: 10px;
}

.vs-card__text p {
    font-size: inherit;
    margin: 0;
    opacity: .8;
}

.bx {
    padding-left: 2px;
    padding-right: 2px;
}

.bx {
    font-family: boxicons !important;
    font-weight: 400;
    font-style: normal;
    font-variant: normal;
    line-height: 1;
    text-rendering: auto;
    display: inline-block;
    text-transform: none;
    speak: none;
    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
}

.vs-card-content {
    --vs-color: var(--vs-primary);
}

.vs-input-parent {
    --vs-color: var(--vs-primary);
    display: -webkit-box;
    display: -ms-flexbox;
    display: flex;
    -ms-flex-item-align: center;
    align-self: center;
    -webkit-box-pack: center;
    -ms-flex-pack: center;
    justify-content: center;
    -webkit-box-orient: vertical;
    -webkit-box-direction: normal;
    -ms-flex-direction: column;
    flex-direction: column;
    position: relative;
}

.vs-input-content {
    display: -webkit-box;
    display: -ms-flexbox;
    display: flex;
    -webkit-box-align: center;
    -ms-flex-align: center;
    align-items: center;
    -webkit-box-pack: start;
    -ms-flex-pack: start;
    justify-content: flex-start;
    position: relative;
    border-radius: 12px;
}

.vs-input-content--has-color .vs-input {
    border-bottom: 2px solid #7d33ff;
}

.vs-input--has-icon--after {
    padding-left: 7px;
    padding-right: 38px;
}

.vs-input__icon__copy {
    cursor: pointer !important;
    top: -15px;
    pointer-events: auto;
}

.vs-input__icon--after {
    left: auto;
    right: 0;
    -webkit-box-shadow: -12px 0 10px -10px rgba(0, 0, 0, var(--vs-shadow-opacity));
    box-shadow: -12px 0 10px -10px rgba(0, 0, 0, var(--vs-shadow-opacity));
}

.vs-input__icon {
    position: absolute;
    right: auto;
    width: 36px;
    height: 36px;
    display: -webkit-box;
    display: -ms-flexbox;
    display: flex;
    -webkit-box-align: center;
    -ms-flex-align: center;
    align-items: center;
    -webkit-box-pack: center;
    -ms-flex-pack: center;
    justify-content: center;
    -webkit-box-shadow: 12px 0 10px -10px rgba(0, 0, 0, var(--vs-shadow-opacity));
    box-shadow: 12px 0 10px -10px rgba(0, 0, 0, var(--vs-shadow-opacity));
    -webkit-transition: all .25s ease;
    transition: all .25s ease;
    border-radius: inherit;
    background: rgba(var(--vs-gray-2), 1);
    pointer-events: none;
    left: 0;
}

.vs-input {
    border: 2px solid transparent;
    background: rgba(var(--vs-gray-2), 1);
    color: rgba(var(--vs-text), 1);
    padding: 7px 13px;
    border-radius: inherit;
    -webkit-transition: all .25s ease;
    transition: all .25s ease;
    padding-left: 10px;
    width: 200px;
}
</style>